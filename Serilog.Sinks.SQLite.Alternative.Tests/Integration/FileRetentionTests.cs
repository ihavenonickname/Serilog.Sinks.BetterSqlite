using System;
using System.IO;
using System.Threading.Tasks;
using Xunit;

namespace Serilog.Sinks.SQLite.Alternative.Tests.Integration;

public class FileRetentionTests : TestBase
{
    private readonly TimeSpan _shortInterval = TimeSpan.FromMilliseconds(10);

    [Theory]
    [InlineData(0, 0)]
    [InlineData(1, 1)]
    [InlineData(5, 5)]
    [InlineData(1, 5)]
    [InlineData(5, 1)]
    public async Task FileDoesNotRotateBelowSizeLimit(int retentionFileCountLimit, int backupFilesToCreate)
    {
        // Arrange
        var loggerConfiguration = new LoggerConfiguration()
            .WriteTo.SQLite(
                logDirectory: _logDirectory,
                batchingOptions: _batchingOptions,
                retentionFileCountLimit: retentionFileCountLimit,
                retentionInterval: _shortInterval);

        for (var i = 0; i < backupFilesToCreate; i++)
        {
            File.Create(Path.Combine(_logDirectory.FullName, $"serilog-logs-{i}.db.backup"));

            await Task.Delay(TimeSpan.FromMilliseconds(5));
        }

        // Act
        await UseLoggerAndWaitALittleBit(loggerConfiguration, async logger =>
        {
            await Task.Delay(_shortInterval * 2);
        });

        // Arrange
        var backupFiles = _logDirectory.GetFiles("*.db.backup");

        Assert.True(backupFiles.Length <= retentionFileCountLimit);
    }
}
