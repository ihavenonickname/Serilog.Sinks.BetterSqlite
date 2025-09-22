using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Sinks.SQLite.Alternative;

var builder = WebApplication.CreateBuilder(args);

Log.Logger = new LoggerConfiguration()
    .WriteTo.SQLite()
    .CreateLogger();

builder.Logging.AddSerilog();

var app = builder.Build();

app.MapGet("/", (ILogger<Program> logger) =>
{
	logger.LogInformation("Custom log");

    return new
    {
    	Hello = "World!"
    };
});

app.Run();
