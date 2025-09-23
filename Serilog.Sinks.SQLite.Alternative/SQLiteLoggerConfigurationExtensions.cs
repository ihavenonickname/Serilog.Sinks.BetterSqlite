using System;
using System.IO;
using Serilog.Configuration;
using Serilog.Core;
using Serilog.Events;

namespace Serilog.Sinks.SQLite.Alternative;

public static class SQLiteLoggerConfigurationExtensions
{
    public static LoggerConfiguration SQLite(
        this LoggerSinkConfiguration sinkConfiguration,
        FileInfo databaseFile,
        TimeZoneInfo? timeZoneInfo = null,
        TimeSpan? fileRotationAgeLimit = null,
        long? fileRotationSizeLimit = null,
        TimeSpan? fileRotationInterval = null,
        int? retentionFileCountLimit = null,
        TimeSpan? retentionInterval = null,
        IFormatProvider? formatProvider = null,
        BatchingOptions? batchingOptions = null,
        LogEventLevel restrictedToMinimumLevel = LevelAlias.Minimum,
        LoggingLevelSwitch? levelSwitch = null)
    {
        var sink = new SQLiteSink(
            databaseFile,
            timeZoneInfo ?? TimeZoneInfo.Utc,
            fileRotationAgeLimit,
            fileRotationSizeLimit,
            fileRotationInterval ?? TimeSpan.FromMinutes(1),
            retentionFileCountLimit,
            retentionInterval ?? TimeSpan.FromMinutes(1),
            formatProvider);

        return sinkConfiguration.Sink(
            sink,
            batchingOptions ?? new(),
            restrictedToMinimumLevel,
            levelSwitch);
    }
}
