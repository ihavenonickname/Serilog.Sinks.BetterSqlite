# Serilog.Sinks.BetterSqlite

[![License: GPL v3](https://img.shields.io/badge/License-GPLv3-blue.svg)](https://www.gnu.org/licenses/gpl-3.0)
![.NET](https://img.shields.io/badge/.NET-6.0%20%7C%207.0%20%7C%208.0-blue)
[![Build](https://github.com/ihavenonickname/Serilog.Sinks.BetterSqlite/actions/workflows/build.yml/badge.svg)](https://github.com/ihavenonickname/Serilog.Sinks.BetterSqlite/actions/workflows/build.yml)

The Serilog.Sinks.BetterSqlite sink provides a robust and flexible mechanism for logging Serilog events directly into a SQLite database. Its design balances performance, configurability, and maintainability.

> **Important note**
>
> This project is under development. It's almost production-ready, but not quite there yet!

## Highlights

- **Batch Logging**: Uses the modern [batching API provided by Serilog](https://github.com/serilog/serilog/pull/2055).
- **Structured Log Storage**: Log events are stored with full structured information, including timestamp, log level, message template, rendered message, and optional trace/span identifiers. Exceptions associated with log events (including all chain of `InnerException`s) are also stored, capturing type, message, stack trace, and source.
- **Time Zone Awareness**: Log timestamps can be configured to use any timezone available in the system.
- **Database Rotation**: The sink supports automatic rotation of the SQLite database based on configurable age and size thresholds.
- **Retention Management**: Archived databases can be automatically pruned.
- **Async-friendly**: Internal implementation uses async APIs that don't block threads.
- **Integration with Microsoft.Extensions.Logging**: The sink can be used seamlessly with Serilog’s Microsoft.Extensions.Logging integration, allowing logs from both application code and framework events to be captured consistently.

## Getting started

Install the [Serilog.Sinks.BetterSqlite package](https://www.nuget.org/packages/Serilog.Sinks.BetterSqlite/) from NuGet:

```bash
dotnet add package Serilog.Sinks.BetterSqlite
```

Then use in your application [like any other Serilog sink](https://github.com/serilog/serilog/wiki/Configuration-Basics#sinks).

Here's a quick ASP.NET app example:

```csharp
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
```

Once you run the app, it creates a SQLite database file named `myapp-logs.db` inside the current working directory.

Of course, that's just the tiniest example. Take a look at the documentation to see more!

## Documentation

The project wiki contains everything you need to know to use this library, including complete sample applications that you can use for reference.

- https://github.com/ihavenonickname/Serilog.Sinks.BetterSqlite/wiki

## Why not `Serilog.Sinks.SQLite`?

There is [another Serilig sink for SQLite](https://github.com/saleem-mirza/serilog-sinks-sqlite) that is older and more popular than this one. So, why should you use `Serilog.Sinks.BetterSqlite` instead?

- `Serilog.Sinks.SQLite` is abandoned:
    - Latest commit is over 2 years old as of the time of the writing
    - Author does not respond to issues anymore
- `Serilog.Sinks.SQLite` lacks important features:
    - No support for any form of file retention policies (only rotation policies)
    - No support for custom timezones (only UTC and Local)
    - No support for customization of batching behavior beyond batch size
- `Serilog.Sinks.SQLite` has dubious design choices:
    - Discards logs if the database file is not rotated
    - Uses blocking APIs instead of asynchronous APIs
    - Implements its own batching mechanism instead of using Serilog's official batching API

`Serilog.Sinks.BetterSqlite` fixes all of these issues.

## Roadmap

- Store log event properties
- Add benchmark tests
- Wirte xmldoc documentation
- Write wiki
- Publish project on NuGet

## License

This project is **free software**, licensed under the [GNU General Public License v3](https://www.gnu.org/licenses/gpl-3.0.html).

You can redistribute it and/or modify it under the terms of the GNU GPL as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.

This program is distributed in the hope that it will be useful, but **WITHOUT ANY WARRANTY**; without even the implied warranty of **MERCHANTABILITY** or **FITNESS FOR A PARTICULAR PURPOSE**. See the [GNU General Public License](https://www.gnu.org/licenses/) for more details.

A copy of the GNU General Public License is provided in the [`LICENSE`](./LICENSE) file in this repository.
