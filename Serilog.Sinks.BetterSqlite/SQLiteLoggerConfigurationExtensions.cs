using System;
using System.IO;
using Serilog.Configuration;
using Serilog.Core;
using Serilog.Events;

namespace Serilog.Sinks.BetterSqlite;

public static class SQLiteLoggerConfigurationExtensions
{
    /// <summary>
    /// Adds a sink that writes log events to a local SQLite database file,
    /// with support for file rotation, retention policies, and batching.
    /// </summary>
    ///
    /// <param name="sinkConfiguration">
    /// The logger sink configuration to extend.
    /// </param>
    ///
    /// <param name="databaseFile">
    /// The path to the SQLite database file where log events and exceptions
    /// will be stored.
    /// </param>
    ///
    /// <param name="timeZoneInfo">
    /// The time zone to use when storing timestamps. If <c>null</c>, defaults
    /// to <see cref="TimeZoneInfo.Utc"/>.
    /// </param>
    ///
    /// <param name="fileRotationAgeLimit">
    /// The maximum age a database file can reach before being rotated. If
    /// <c>null</c>, no age-based rotation occurs.
    /// </param>
    ///
    /// <param name="fileRotationSizeLimit">
    /// The maximum file size, in bytes, that a database file can reach before
    /// being rotated. If <c>null</c>, no size-based rotation occurs.
    /// </param>
    ///
    /// <param name="fileRotationInterval">
    /// The interval at which the file rotation policy is checked and
    /// applied. If <c>null</c>, defaults to one minute.
    /// </param>
    ///
    /// <param name="retentionFileCountLimit">
    /// The maximum number of rotated database files to retain. If <c>null</c>,
    /// no retention limit applies.
    /// </param>
    ///
    /// <param name="retentionInterval">
    /// The interval at which the retention policy is checked and applied. If
    /// <c>null</c>, defaults to one minute.
    /// </param>
    ///
    /// <param name="formatProvider">
    /// Supplies culture-specific formatting information. If <c>null</c>,
    /// defaults to the current culture.
    /// </param>
    ///
    /// <param name="batchingOptions">
    /// Configures the batching behavior of the sink. If <c>null</c>, defaults
    /// to a new instance of <see cref="BatchingOptions"/>.
    /// </param>
    ///
    /// <param name="restrictedToMinimumLevel">
    /// The minimum level for events passed through the sink. Ignored when
    /// <paramref name="levelSwitch"/> is specified.
    /// </param>
    ///
    /// <param name="levelSwitch">
    /// A switch allowing the pass-through minimum level to be changed at
    /// runtime.
    /// </param>
    ///
    /// <returns>
    /// The same <see cref="LoggerConfiguration"/> object allowing further
    /// configuration of the logger.
    /// </returns>
    ///
    /// <remarks>
    /// See <see href="https://github.com/ihavenonickname/Serilog.Sinks.BetterSqlite/wiki">the official wiki</see>
    /// for usage examples and detailed documentation.
    /// </remarks>
    ///
    /// <example>
    /// <code>
    /// Log.Logger = new LoggerConfiguration()
    ///     .WriteTo.SQLite(new FileInfo("logs.db"))
    ///     .CreateLogger();
    /// </code>
    /// </example>
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
        if (sinkConfiguration is null)
        {
            throw new ArgumentNullException(nameof(sinkConfiguration));
        }

        if (databaseFile is null)
        {
            throw new ArgumentNullException(nameof(databaseFile));
        }

        if (!Enum.IsDefined(restrictedToMinimumLevel))
        {
            throw new ArgumentException("Invalid enum value", nameof(databaseFile));
        }

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
