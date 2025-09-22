# Serilog.Sinks.SQLite.Alternative

The Serilog.Sinks.SQLite.Alternative sink provides a robust and flexible mechanism for logging Serilog events directly into a SQLite database. Its design balances performance, configurability, and maintainability.

## Highlights

- **Batch Logging**: Log events are buffered and written to the database in batches, reducing I/O overhead and improving overall performance. The batch size and flush interval can be configured to balance latency and throughput.
- **Structured Log Storage**: Log entries store full structured information, including timestamp, log level, message template, rendered message, and optional trace/span identifiers. Exceptions associated with log events are stored in a separate table, capturing type, message, stack trace, and source.
- **Time Zone Awareness**: Timestamps are automatically converted to a configurable TimeZoneInfo, enabling consistent log reporting across different environments and regions.
- **Database Rotation**: The sink supports automatic rotation of the SQLite database based on configurable age and file size thresholds. Upon rotation, the current database is archived, and a new database is created for subsequent log entries. This helps prevent unbounded growth of the log store.
- **Retention Management**: Archived databases can be automatically pruned according to retention rules, including maximum file count or retention interval. This ensures long-term log storage does not consume excessive disk space.
- **Concurrency-Safe Writes**: Internal synchronization ensures that multiple batches are written safely, maintaining database consistency without requiring external locking mechanisms.
- **Integration with Microsoft.Extensions.Logging**: The sink can be used seamlessly with Serilog’s Microsoft.Extensions.Logging integration, allowing logs from both application code and framework events to be captured consistently.
- **Configurable Formatting**: Message rendering can leverage a custom IFormatProvider if needed, giving control over formatting of culture-sensitive values.
- **Lightweight and Self-Contained**: The sink depends solely on SQLite and standard .NET libraries, avoiding external dependencies while providing all core logging capabilities.

## Getting started

Install the [Serilog.Sinks.SQLite.Alternative package](https://www.nuget.org/packages/Serilog.Sinks.SQLite.Alternative/) from NuGet:

```bash
dotnet add package Serilog.Sinks.SQLite.Alternative
```

Then use in your application [like any other Serilog sink](https://github.com/serilog/serilog/wiki/Configuration-Basics#sinks).

Here's a quick ASP.NET app example:

```csharp
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
```

Once you run the app, it'll create a `logs` directory in your current working directly, and a SQLite database inside it.

Of course, that's just the default behavior. Take a look at the documentation to see how to configure the library.

## Documentation

The project wiki contains everything you need to know to use this library, including complete sample applications that you can use for reference.

- https://github.com/ihavenonickname/Serilog.Sinks.Sqlite.Alternative/wiki

## Why not `Serilog.Sinks.SQLite`?

TODO

## Roadmap

TODO

## License

This project is **free software**, licensed under the [GNU General Public License v3](https://www.gnu.org/licenses/gpl-3.0.html).

You can redistribute it and/or modify it under the terms of the GNU GPL as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.

This program is distributed in the hope that it will be useful, but **WITHOUT ANY WARRANTY**; without even the implied warranty of **MERCHANTABILITY** or **FITNESS FOR A PARTICULAR PURPOSE**. See the [GNU General Public License](https://www.gnu.org/licenses/) for more details.

A copy of the GNU General Public License is provided in the [`LICENSE`](./LICENSE) file in this repository. If not, see <https://www.gnu.org/licenses/>.
