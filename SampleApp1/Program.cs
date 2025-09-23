using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Sinks.SQLite.Alternative;
using System.IO;

var builder = WebApplication.CreateBuilder(args);

Log.Logger = new LoggerConfiguration()
    .WriteTo.SQLite(databaseFile: new FileInfo("myapp-logs.db"))
    .CreateLogger();

builder.Logging.AddSerilog();

var app = builder.Build();

app.MapGet("/", (ILogger<Program> logger) =>
{
	logger.LogInformation("Hello, SQLite!");

    return new
    {
    	Hello = "World"
    };
});

app.Run();

Log.CloseAndFlush();
