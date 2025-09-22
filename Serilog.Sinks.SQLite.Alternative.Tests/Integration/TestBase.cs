using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using Serilog.Configuration;
using Serilog.Core;

namespace Serilog.Sinks.SQLite.Alternative.Tests.Integration;

public abstract class TestBase
{
    protected readonly DirectoryInfo _logDirectory;
    protected readonly BatchingOptions _batchingOptions = new()
    {
        BatchSizeLimit = 1,
    };

    public TestBase()
    {
        _logDirectory = new(Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}"));
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
                DataSource = _logDirectory.EnumerateFiles("*.db").First().FullName
            }.ConnectionString
        };

        connection.Open();

        command.Connection = connection;

        using var reader = command.ExecuteReader();

        useReader(reader);

        command.Dispose();
    }
}
