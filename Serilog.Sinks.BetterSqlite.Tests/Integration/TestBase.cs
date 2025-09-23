using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using Serilog.Configuration;
using Serilog.Core;

namespace Serilog.Sinks.BetterSqlite.Tests.Integration;

public abstract class TestBase
{
    protected readonly FileInfo _databaseFile;
    protected readonly BatchingOptions _batchingOptions = new()
    {
        BatchSizeLimit = 1,
    };

    public TestBase()
    {
        var guid = Guid.NewGuid();

        _databaseFile = new(Path.Combine(Path.GetTempPath(), $"{guid}", $"serilog-sinks-bettersqlite-{guid}.db"));
    }

    protected async Task UseLoggerAndWaitALittleBit(LoggerConfiguration loggerConfiguration, Func<Logger, Task> useLogger)
    {
        using (var logger = loggerConfiguration.CreateLogger())
        {
            await useLogger(logger);
        }

        await Task.Delay(TimeSpan.FromSeconds(1));
    }

    protected void ExecuteSelect(SqliteCommand command, Action<SqliteDataReader> useReader)
    {
        using var connection = new SqliteConnection()
        {
            ConnectionString = new SqliteConnectionStringBuilder()
            {
                DataSource = _databaseFile.FullName
            }.ConnectionString
        };

        connection.Open();

        command.Connection = connection;

        using var reader = command.ExecuteReader();

        useReader(reader);

        command.Dispose();
    }
}
