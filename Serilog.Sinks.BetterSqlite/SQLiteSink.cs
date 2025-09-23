using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using Serilog.Core;
using Serilog.Events;

namespace Serilog.Sinks.BetterSqlite;

internal class SQLiteSink : IBatchedLogEventSink, IDisposable
{
    private readonly FileInfo _databaseFile;
    private readonly TimeZoneInfo _timeZoneInfo;
    private readonly TimeSpan? _fileRotationAgeLimit;
    private readonly long? _fileRotationSizeLimit;
    private readonly TimeSpan _fileRotationInterval;
    private readonly int? _retentionFileCountLimit;
    private readonly TimeSpan _retentionInterval;
    private readonly IFormatProvider? _formatProvider;

    private readonly DirectoryInfo _databaseDirectory;
    private readonly SqliteConnection _connection;
    private readonly SqliteCommand _insertLogCommand;
    private readonly SqliteCommand _insertExceptionCommand;
    private readonly SqliteCommand _selectOldestLogTimestamp;
    private readonly Task? _taskRetention;
    private readonly Task? _taskRotation;

    private readonly SemaphoreSlim _semaphore = new(1, 1);
    private readonly CancellationTokenSource _cts = new();

    internal SQLiteSink(
        FileInfo databaseFile,
        TimeZoneInfo timeZoneInfo,
        TimeSpan? fileRotationAgeLimit,
        long? fileRotationSizeLimit,
        TimeSpan fileRotationInterval,
        int? retentionFileCountLimit,
        TimeSpan retentionInterval,
        IFormatProvider? formatProvider)
    {
        _databaseFile = databaseFile;
        _timeZoneInfo = timeZoneInfo;
        _fileRotationAgeLimit = fileRotationAgeLimit;
        _fileRotationSizeLimit = fileRotationSizeLimit;
        _fileRotationInterval = fileRotationInterval;
        _retentionFileCountLimit = retentionFileCountLimit;
        _retentionInterval = retentionInterval;
        _formatProvider = formatProvider;

        if (_databaseFile.Directory is null)
        {
            throw new ArgumentException("Could not find the database directory");
        }

        _databaseDirectory = _databaseFile.Directory;
        _databaseDirectory.Create();

        _connection = new(new SqliteConnectionStringBuilder()
        {
            DataSource = _databaseFile.FullName,
        }.ConnectionString);

        _connection.Open();

        using var command = _connection.CreateCommand();

        command.CommandText = @"
            create table if not exists logs (
                id integer primary key autoincrement,
                timestamp text not null,
                level text not null,
                message text not null,
                message_template text not null,
                span_id text,
                trace_id text
            );

            create table if not exists exceptions (
                id integer primary key autoincrement,
                log_id integer not null,
                type text not null,
                message text not null,
                stacktrace text,
                source text
            )";

        command.ExecuteNonQuery();

        _insertLogCommand = _connection.CreateCommand();
        _insertLogCommand.Parameters.Add("$timestamp", SqliteType.Text);
        _insertLogCommand.Parameters.Add("$level", SqliteType.Text);
        _insertLogCommand.Parameters.Add("$message_template", SqliteType.Text);
        _insertLogCommand.Parameters.Add("$message", SqliteType.Text);
        _insertLogCommand.Parameters.Add("$span_id", SqliteType.Text);
        _insertLogCommand.Parameters.Add("$trace_id", SqliteType.Text);
        _insertLogCommand.CommandText = @"
            insert into logs (timestamp, level, message_template, message, span_id, trace_id)
            values ($timestamp, $level, $message_template, $message, $span_id, $trace_id)
            returning id;
        ";

        _insertExceptionCommand = _connection.CreateCommand();
        _insertExceptionCommand.Parameters.Add("$log_id", SqliteType.Integer);
        _insertExceptionCommand.Parameters.Add("$type", SqliteType.Text);
        _insertExceptionCommand.Parameters.Add("$message", SqliteType.Text);
        _insertExceptionCommand.Parameters.Add("$stacktrace", SqliteType.Text);
        _insertExceptionCommand.Parameters.Add("$source", SqliteType.Text);
        _insertExceptionCommand.CommandText = @"
            insert into exceptions (log_id, type, message, stacktrace, source)
            values ($log_id, $type, $message, $stacktrace, $source);
        ";

        _selectOldestLogTimestamp = _connection.CreateCommand();
        _selectOldestLogTimestamp.CommandText = "select min(timestamp) from logs;";

        if (_retentionFileCountLimit is not null)
        {
            _taskRetention = Task.Run(async () =>
            {
                while (!_cts.IsCancellationRequested)
                {
                    await _semaphore.WaitAsync(_cts.Token);

                    try
                    {
                        CheckAndApplyRetention();
                    }
                    finally
                    {
                        _semaphore.Release();
                    }

                    await Task.Delay(_retentionInterval, _cts.Token);
                }
            });
        }

        if (_fileRotationAgeLimit is not null || _fileRotationSizeLimit is not null)
        {
            _taskRotation = Task.Run(async () =>
            {
                while (!_cts.IsCancellationRequested)
                {
                    await _semaphore.WaitAsync(_cts.Token);

                    try
                    {
                        await CheckAndApplyRotation();
                    }
                    finally
                    {
                        _semaphore.Release();
                    }

                    await Task.Delay(_fileRotationInterval, _cts.Token);
                }
            });
        }
    }

    public async Task EmitBatchAsync(IReadOnlyCollection<LogEvent> batch)
    {
        SqliteTransaction? transaction = null;

        await _semaphore.WaitAsync(_cts.Token);

        try
        {
            transaction = (SqliteTransaction)await _connection.BeginTransactionAsync(_cts.Token);

            _insertLogCommand.Transaction = transaction;
            _insertExceptionCommand.Transaction = transaction;

            foreach (var logEvent in batch)
            {
                var dt = TimeZoneInfo.ConvertTimeFromUtc(logEvent.Timestamp.UtcDateTime, _timeZoneInfo);

                _insertLogCommand.Parameters["$timestamp"].Value = dt.ToString("yyyy-MM-ddTHH:mm:ss.fffzzz");
                _insertLogCommand.Parameters["$level"].Value = Enum.GetName(logEvent.Level);
                _insertLogCommand.Parameters["$message_template"].Value = logEvent.MessageTemplate.Text;
                _insertLogCommand.Parameters["$message"].Value = logEvent.RenderMessage(_formatProvider);
                _insertLogCommand.Parameters["$span_id"].Value = (object?)logEvent.SpanId?.ToString() ?? DBNull.Value;
                _insertLogCommand.Parameters["$trace_id"].Value = (object?)logEvent.TraceId?.ToString() ?? DBNull.Value;

                var logId = await _insertLogCommand.ExecuteScalarAsync(_cts.Token);

                for (var exception = logEvent.Exception; exception != null; exception = exception.InnerException)
                {
                    _insertExceptionCommand.Parameters["$log_id"].Value = logId;
                    _insertExceptionCommand.Parameters["$type"].Value = exception.GetType().FullName;
                    _insertExceptionCommand.Parameters["$message"].Value = exception.Message;
                    _insertExceptionCommand.Parameters["$stacktrace"].Value = (object?)exception.StackTrace ?? DBNull.Value;
                    _insertExceptionCommand.Parameters["$source"].Value = (object?)exception.Source ?? DBNull.Value;

                    await _insertExceptionCommand.ExecuteNonQueryAsync(_cts.Token);
                }
            }

            await transaction.CommitAsync(_cts.Token);
        }
        finally
        {
            if (transaction is not null)
            {
                await transaction.DisposeAsync();
            }

            _semaphore.Release();
        }
    }

    public void Dispose()
    {
        _cts.Cancel();

        try
        {
            _taskRotation?.Wait();
        }
        catch (AggregateException ex) when (ex.InnerException is TaskCanceledException)
        { }

        try
        {
            _taskRetention?.Wait();
        }
        catch (AggregateException ex) when (ex.InnerException is TaskCanceledException)
        { }

        _semaphore.Wait();

        try
        {
            _connection.Dispose();
        }
        finally
        {
            _semaphore.Release();
        }

        _taskRotation?.Dispose();
        _taskRetention?.Dispose();
        _semaphore.Dispose();
        _cts.Dispose();
    }

    private void CheckAndApplyRetention()
    {
        var backupFiles = _databaseDirectory.GetFiles($"backup-*-{_databaseFile.Name}");

        var filesToDelete = backupFiles.Length - _retentionFileCountLimit;

        if (filesToDelete < 1)
        {
            return;
        }

        Array.Sort(backupFiles, (x, y) => x.CreationTimeUtc.CompareTo(y.CreationTimeUtc));

        for (var i = 0; i < filesToDelete && !_cts.IsCancellationRequested; i++)
        {
            try
            {
                backupFiles[i].Delete();
            }
            catch (Exception ex)
            {
                Debugging.SelfLog.WriteLine("Failed to delete backup file {0}: {1}", backupFiles[i].FullName, ex);
            }
        }
    }

    private async Task CheckAndApplyRotation()
    {
        var now = DateTimeOffset.UtcNow;

        var shouldRotate = false;

        if (_fileRotationAgeLimit is not null)
        {
            await using var reader = await _selectOldestLogTimestamp.ExecuteReaderAsync(_cts.Token);

            if (await reader.ReadAsync(_cts.Token) && !reader.IsDBNull(0) && now.Subtract(reader.GetDateTimeOffset(0)) > _fileRotationAgeLimit)
            {
                Debugging.SelfLog.WriteLine("Rotating file due to age limit");

                shouldRotate = true;
            }
        }
        else if (_fileRotationSizeLimit is not null)
        {
            _databaseFile.Refresh();

            if (_databaseFile.Length > _fileRotationSizeLimit)
            {
                Debugging.SelfLog.WriteLine("Rotating file due to size limit");

                shouldRotate = true;
            }
        }

        if (!shouldRotate || _cts.IsCancellationRequested)
        {
            return;
        }

        var timestamp = now.ToString("yyyy_MM_dd_HH_mm_ss_fff");
        var backupFullPath = Path.Combine(_databaseDirectory.FullName, $"backup-{timestamp}-{_databaseFile.Name}").Replace("'", "''");

        try
        {
            using (var command = _connection.CreateCommand())
            {
                command.CommandText = $"vacuum into '{backupFullPath}';";

                await command.ExecuteNonQueryAsync(_cts.Token);
            }

            using (var command = _connection.CreateCommand())
            {
                command.CommandText = @$"
                    delete from exceptions;
                    delete from logs;
                    delete from sqlite_sequence where name in ('logs', 'exceptions');
                ";

                await command.ExecuteNonQueryAsync(_cts.Token);
            }

            using (var command = _connection.CreateCommand())
            {
                command.CommandText = "vacuum;";

                await command.ExecuteNonQueryAsync(_cts.Token);
            }
        }
        catch (Exception ex)
        {
            Debugging.SelfLog.WriteLine("Error rotating database: {0}", ex);
        }
    }
}
