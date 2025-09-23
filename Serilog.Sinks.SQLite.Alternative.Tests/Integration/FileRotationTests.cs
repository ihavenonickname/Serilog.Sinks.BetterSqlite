using System;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Serilog.Sinks.SQLite.Alternative.Tests.Integration;

public class FileRotationTests : TestBase
{
    private readonly string _largeMessage = new StringBuilder().Insert(0, "🚀", 9999).ToString();
    private readonly long _oneMegabyte = 1024 * 1024;
    private readonly TimeSpan _shortInterval = TimeSpan.FromMilliseconds(10);

    [Fact]
    public async Task FileRotatesOnSizeLimit()
    {
        // Arrange
        var loggerConfiguration = new LoggerConfiguration()
            .WriteTo.SQLite(
                databaseFile: _databaseFile,
                batchingOptions: _batchingOptions,
                fileRotationSizeLimit: _oneMegabyte,
                fileRotationInterval: _shortInterval);

        // Act
        await UseLoggerAndWaitALittleBit(loggerConfiguration, async logger =>
        {
            for (var i = 0; i < 15; i++)
            {
                logger.Information(_largeMessage);
            }

            await Task.Delay(_shortInterval * 5);

            for (var i = 0; i < 15; i++)
            {
                logger.Information(_largeMessage);
            }

            await Task.Delay(_shortInterval * 5);

            for (var i = 0; i < 15; i++)
            {
                logger.Information(_largeMessage);
            }

            await Task.Delay(_shortInterval * 5);

        });

        // Assert
        Assert.NotNull(_databaseFile.Directory);

        var backupFiles = _databaseFile.Directory.GetFiles($"backup-*-{_databaseFile.Name}");

        Assert.Equal(3, backupFiles.Length);
    }

    [Fact]
    public async Task FileDoesNotRotateBelowSizeLimit()
    {
        // Arrange
        var loggerConfiguration = new LoggerConfiguration()
            .WriteTo.SQLite(
                databaseFile: _databaseFile,
                batchingOptions: _batchingOptions,
                fileRotationSizeLimit: _oneMegabyte,
                fileRotationInterval: _shortInterval);

        // Act
        await UseLoggerAndWaitALittleBit(loggerConfiguration, async logger =>
        {
            logger.Information(_largeMessage);

            await Task.Delay(_shortInterval * 5);

            logger.Information(_largeMessage);

            await Task.Delay(_shortInterval * 5);

            logger.Information(_largeMessage);

            await Task.Delay(_shortInterval * 5);
        });

        // Assert
        Assert.NotNull(_databaseFile.Directory);

        Assert.Empty(_databaseFile.Directory.GetFiles($"backup-*-{_databaseFile.Name}"));
    }

    [Fact]
    public async Task FileRotatesOnAgeLimit()
    {
        // Arrange
        var ageLimit = TimeSpan.FromMilliseconds(100);

        var loggerConfiguration = new LoggerConfiguration()
            .WriteTo.SQLite(
                databaseFile: _databaseFile,
                batchingOptions: _batchingOptions,
                fileRotationAgeLimit: ageLimit,
                fileRotationInterval: _shortInterval);

        // Act
        await UseLoggerAndWaitALittleBit(loggerConfiguration, async logger =>
        {
            logger.Information(_largeMessage);

            await Task.Delay(ageLimit + _shortInterval * 5);

            logger.Information(_largeMessage);

            await Task.Delay(ageLimit + _shortInterval * 5);

            logger.Information(_largeMessage);

            await Task.Delay(ageLimit + _shortInterval * 5);
        });

        // Assert
        Assert.NotNull(_databaseFile.Directory);

        var backupFiles = _databaseFile.Directory.GetFiles($"backup-*-{_databaseFile.Name}");

        Assert.Equal(3, backupFiles.Length);
    }

    [Fact]
    public async Task FileRotatesOnAgeLimitWithCustomTimeZone()
    {
        // Arrange
        var ageLimit = TimeSpan.FromMilliseconds(100);

        var loggerConfiguration = new LoggerConfiguration()
            .WriteTo.SQLite(
                databaseFile: _databaseFile,
                batchingOptions: _batchingOptions,
                fileRotationAgeLimit: ageLimit,
                fileRotationInterval: _shortInterval,
                timeZoneInfo: TimeZoneInfo.FindSystemTimeZoneById("America/Sao_Paulo"));

        // Act
        await UseLoggerAndWaitALittleBit(loggerConfiguration, async logger =>
        {
            logger.Information(_largeMessage);

            await Task.Delay(ageLimit + _shortInterval * 5);

            logger.Information(_largeMessage);

            await Task.Delay(ageLimit + _shortInterval * 5);

            logger.Information(_largeMessage);

            await Task.Delay(ageLimit + _shortInterval * 5);
        });

        // Assert
        Assert.NotNull(_databaseFile.Directory);

        var backupFiles = _databaseFile.Directory.GetFiles($"backup-*-{_databaseFile.Name}");

        Assert.Equal(3, backupFiles.Length);
    }

    [Fact]
    public async Task FileDoesNotRotateBelowAgeLimit()
    {
        // Arrange
        var ageLimit = TimeSpan.FromDays(1);

        var loggerConfiguration = new LoggerConfiguration()
            .WriteTo.SQLite(
                databaseFile: _databaseFile,
                batchingOptions: _batchingOptions,
                fileRotationAgeLimit: ageLimit,
                fileRotationInterval: _shortInterval);

        // Act
        await UseLoggerAndWaitALittleBit(loggerConfiguration, async logger =>
        {
            logger.Information(_largeMessage);

            await Task.Delay(_shortInterval * 5);
        });

        // Assert
        Assert.NotNull(_databaseFile.Directory);

        Assert.Empty(_databaseFile.Directory.GetFiles($"backup-*-{_databaseFile.Name}"));
    }
}
