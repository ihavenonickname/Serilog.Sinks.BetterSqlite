using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using Xunit;

namespace Serilog.Sinks.BetterSqlite.Tests.Integration;

public class BasicTests : TestBase
{
    [Fact]
    public async Task SinkInitializationCreatesEmptyDatabase()
    {
        // Arrange
        var loggerConfiguration = new LoggerConfiguration()
            .WriteTo.SQLite(
                databaseFile: _databaseFile,
                batchingOptions: _batchingOptions);

        // Act
        await UseLoggerAndWaitALittleBit(loggerConfiguration, _ => Task.CompletedTask);

        // Assert
        Assert.NotNull(_databaseFile.Directory);
        Assert.True(_databaseFile.Exists);
        Assert.Empty(_databaseFile.Directory.EnumerateDirectories());

        var files = _databaseFile.Directory.EnumerateFiles().ToArray();

        using var command = new SqliteCommand
        {
            CommandText = "select * from logs"
        };

        ExecuteSelect(command, reader =>
        {
            Assert.False(reader.HasRows);
        });
    }

    [Fact]
    public async Task LogsHaveSequentialIds()
    {
        // Arrange
        var loggerConfiguration = new LoggerConfiguration()
            .WriteTo.SQLite(
                databaseFile: _databaseFile,
                batchingOptions: _batchingOptions);

        // Act
        await UseLoggerAndWaitALittleBit(loggerConfiguration, logger =>
        {
            logger.Information("test1");
            logger.Information("test2");
            logger.Information("test3");

            return Task.CompletedTask;
        });

        // Assert
        using var command = new SqliteCommand
        {
            CommandText = "select id from logs order by id"
        };

        ExecuteSelect(command, reader =>
        {
            Assert.True(reader.Read());
            Assert.Equal(1, reader.GetInt64(0));

            Assert.True(reader.Read());
            Assert.Equal(2, reader.GetInt64(0));

            Assert.True(reader.Read());
            Assert.Equal(3, reader.GetInt64(0));

            Assert.False(reader.Read());
        });
    }

    [Fact]
    public async Task LogsStoreCorrectLevel()
    {
        // Arrange
        var loggerConfiguration = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .WriteTo.SQLite(
                databaseFile: _databaseFile,
                batchingOptions: _batchingOptions);

        // Act
        await UseLoggerAndWaitALittleBit(loggerConfiguration, logger =>
        {
            logger.Debug("debug");
            logger.Information("information");
            logger.Warning("warning");
            logger.Error("error");

            return Task.CompletedTask;
        });

        // Assert
        using var command = new SqliteCommand
        {
            CommandText = "select level from logs order by id"
        };

        ExecuteSelect(command, reader =>
        {
            Assert.True(reader.Read());
            Assert.Equal("Debug", reader.GetString(0));
            Assert.True(reader.Read());
            Assert.Equal("Information", reader.GetString(0));

            Assert.True(reader.Read());
            Assert.Equal("Warning", reader.GetString(0));
            Assert.True(reader.Read());
            Assert.Equal("Error", reader.GetString(0));

            Assert.False(reader.Read());
        });
    }

    [Fact]
    public async Task LogsStoreBothMessageAndTemplate()
    {
        // Arrange
        var loggerConfiguration = new LoggerConfiguration()
            .WriteTo.SQLite(
                databaseFile: _databaseFile,
                batchingOptions: _batchingOptions);

        // Act
        await UseLoggerAndWaitALittleBit(loggerConfiguration, logger =>
        {
            logger.Information("test 1");
            logger.Information("test {SomeArgument}", 2);

            return Task.CompletedTask;
        });

        // Assert
        using var command = new SqliteCommand
        {
            CommandText = "select message, message_template from logs order by id"
        };

        ExecuteSelect(command, reader =>
        {
            Assert.True(reader.Read());
            Assert.Equal("test 1", reader.GetString(0));
            Assert.Equal("test 1", reader.GetString(1));

            Assert.True(reader.Read());
            Assert.Equal("test 2", reader.GetString(0));
            Assert.Equal("test {SomeArgument}", reader.GetString(1));

            Assert.False(reader.Read());
        });
    }

    [Fact]
    public async Task LogsStoreSpanIdAndTraceIdIfAvailable()
    {
        // Arrange
        var loggerConfiguration = new LoggerConfiguration()
            .WriteTo.SQLite(
                databaseFile: _databaseFile,
                batchingOptions: _batchingOptions);

        var activity = new Activity("dummy");

        // Act
        await UseLoggerAndWaitALittleBit(loggerConfiguration, logger =>
        {
            using (activity)
            {
                activity.Start();

                logger.Information("test 1");

                activity.Stop();
            }

            logger.Information("test 2");

            return Task.CompletedTask;
        });

        // Assert
        using var command = new SqliteCommand
        {
            CommandText = "select span_id, trace_id from logs order by id"
        };

        ExecuteSelect(command, reader =>
        {
            Assert.True(reader.Read());
            Assert.Equal($"{activity.SpanId}", reader.GetString(0));
            Assert.Equal($"{activity.TraceId}", reader.GetString(1));

            Assert.True(reader.Read());
            Assert.Equal(DBNull.Value, reader.GetValue(0));
            Assert.Equal(DBNull.Value, reader.GetValue(1));

            Assert.False(reader.Read());
        });
    }

    [Fact]
    public async Task LogsStoreExceptionsIfAvailable()
    {
        // Arrange
        var loggerConfiguration = new LoggerConfiguration()
            .WriteTo.SQLite(
                databaseFile: _databaseFile,
                batchingOptions: _batchingOptions);

        var exception = new InvalidOperationException(
            "exception 2",
            new OutOfMemoryException("exception 1"));

        // Act
        await UseLoggerAndWaitALittleBit(loggerConfiguration, logger =>
        {
            logger.Error(exception, "some error");

            return Task.CompletedTask;
        });

        // Assert
        using var command = new SqliteCommand
        {
            CommandText = "select id, log_id, type, message from exceptions order by id"
        };

        ExecuteSelect(command, reader =>
        {
            Assert.True(reader.Read());
            Assert.Equal(1, reader.GetInt64(0));
            Assert.Equal(1, reader.GetInt64(1));
            Assert.Equal(exception.GetType().FullName, reader.GetString(2));
            Assert.Equal(exception.Message, reader.GetString(3));

            Assert.True(reader.Read());
            Assert.Equal(2, reader.GetInt64(0));
            Assert.Equal(1, reader.GetInt64(1));
            Assert.Equal(exception.InnerException!.GetType().FullName, reader.GetString(2));
            Assert.Equal(exception.InnerException.Message, reader.GetString(3));

            Assert.False(reader.Read());
        });
    }
}
