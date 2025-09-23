using System;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using Xunit;

namespace Serilog.Sinks.BetterSqlite.Tests.Integration;

public class TimeZoneTests : TestBase
{
    [Fact]
    public async Task SinkUsesUtcByDefault()
    {
        // Arrange
        var loggerConfiguration = new LoggerConfiguration()
            .WriteTo.SQLite(
                databaseFile: _databaseFile,
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

    [Theory]
    [InlineData("America/Sao_Paulo")]
    [InlineData("Asia/Tokyo")]
    [InlineData("Europe/London")]
    [InlineData("America/New_York")]
    [InlineData("Australia/Sydney")]
    public async Task SinkAppliesConfiguredTimezone(string timezoneId)
    {
        // Arrange
        var timeZoneInfo = TimeZoneInfo.FindSystemTimeZoneById(timezoneId);

        var loggerConfiguration = new LoggerConfiguration()
            .WriteTo.SQLite(
                databaseFile: _databaseFile,
                batchingOptions: _batchingOptions,
                timeZoneInfo: timeZoneInfo);

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
            Assert.Equal(timeZoneInfo.GetUtcOffset(DateTime.UtcNow), dateTimeOffset.Offset);
        });
    }
}
