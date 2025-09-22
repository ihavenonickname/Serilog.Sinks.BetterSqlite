using System;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Xunit;

namespace Serilog.Sinks.SQLite.Alternative.Tests.Integration;

public class MicrosoftLoggerTests : TestBase
{
    [Fact]
    public async Task LogsWorkWithMicrosoftILogger()
    {
        // Arrange
        var builder = Host.CreateApplicationBuilder();

        Log.Logger = new LoggerConfiguration()
            .WriteTo.SQLite(
                logDirectory: _logDirectory,
                batchingOptions: _batchingOptions)
            .CreateLogger();

        builder.Logging.AddSerilog();

        var app = builder.Build();

        var message = "hello from Microsoft.Extensions.Logging.ILogger";

        // Act
        using (var scope = app.Services.CreateScope())
        {
            var logger = scope.ServiceProvider.GetRequiredService<ILogger<MicrosoftLoggerTests>>();

            logger.LogInformation(message);
        }

        await Task.Delay(TimeSpan.FromSeconds(1));

        // Assert
        using var command = new SqliteCommand
        {
            CommandText = "select message from logs"
        };

        ExecuteSelect(command, reader =>
        {
            Assert.True(reader.Read());
            Assert.Equal(message, reader.GetString(0));
            Assert.False(reader.Read());
        });
    }
}
