using System;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using Xunit;

namespace Serilog.Sinks.SQLite.Alternative.Tests.Integration;

public class TimeZoneTests : TestBase
{
    [Fact]
    public async Task SinkUsesUtcByDefault()
    {
        // Arrange
        var loggerConfiguration = new LoggerConfiguration()
            .WriteTo.SQLite(
                logDirectory: _logDirectory,
                batchingOptions: _batchingOptions);

        // Act
        await UseLoggerAndWaitALittleBit(loggerConfiguration, logger =>
        {
            logger.Information("test");

            return Task.CompletedTask;
        });

        // Assert
        using var command = new SqliteCommand
        {
            CommandText = "select timestamp from logs"
        };

        ExecuteSelect(command, reader =>
        {
            reader.Read();

            var dateTimeOffset = reader.GetDateTimeOffset(0);

            Assert.Equal(DateTime.UtcNow, dateTimeOffset.UtcDateTime, TimeSpan.FromSeconds(5));
            Assert.Equal(TimeSpan.Zero, dateTimeOffset.Offset);
        });
    }

    [Fact]
    public async Task SinkAppliesConfiguredTimezone()
    {
        // Arrange
        var loggerConfiguration = new LoggerConfiguration()
            .WriteTo.SQLite(
                logDirectory: _logDirectory,
                batchingOptions: _batchingOptions,
                timeZoneInfo: TimeZoneInfo.FindSystemTimeZoneById("America/Sao_Paulo"));

        // Act
        await UseLoggerAndWaitALittleBit(loggerConfiguration, logger =>
        {
            logger.Information("test");

            return Task.CompletedTask;
        });

        // Assert
        using var command = new SqliteCommand
        {
            CommandText = "select timestamp from logs"
        };

        ExecuteSelect(command, reader =>
        {
            reader.Read();

            var dateTimeOffset = reader.GetDateTimeOffset(0);

            Assert.Equal(DateTime.UtcNow, dateTimeOffset.UtcDateTime, TimeSpan.FromSeconds(5));
            Assert.Equal(TimeSpan.FromHours(-3), dateTimeOffset.Offset);
        });
    }
}
