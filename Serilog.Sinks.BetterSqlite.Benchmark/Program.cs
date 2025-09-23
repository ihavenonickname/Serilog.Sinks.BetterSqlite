using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Reports;
using BenchmarkDotNet.Running;
using Serilog.Configuration;
using System;
using System.IO;
using System.Threading.Tasks;

namespace Serilog.Sinks.BetterSqlite.Benchmark;

public class LogsPerSecondColumn : IColumn
{
    public string Id => "Logs/sec";
    public string ColumnName => "Logs/sec";
    public bool AlwaysShow => true;
    public ColumnCategory Category => ColumnCategory.Custom;
    public int PriorityInCategory => 0;
    public bool IsNumeric => true;
    public UnitType UnitType => UnitType.Dimensionless;

    public string GetValue(Summary summary, BenchmarkCase benchmarkCase)
    {
        double meanNanosec = summary[benchmarkCase]!.ResultStatistics?.Mean ?? 0;

        if (meanNanosec <= 0)
        {
             return "-";
        }

        var logsPerSecond = BetterSqliteBenchmarks.LOGS_PER_ITER / (meanNanosec / 1_000_000_000.0);

        return logsPerSecond.ToString("N0");
    }

    public string GetValue(Summary summary, BenchmarkCase benchmarkCase, SummaryStyle style) => GetValue(summary, benchmarkCase);
    public bool IsAvailable(Summary summary) => true;
    public bool IsDefault(Summary summary, BenchmarkCase benchmarkCase) => false;
    public string Legend => $"Logs per second that were stored in the database";
    public override string ToString() => ColumnName;
}

[WarmupCount(1)]
[IterationCount(3)]
[InvocationCount(1)]
public class BetterSqliteBenchmarks
{
    public const int LOGS_PER_ITER = 5000;

    [Params(1, 100, 1000)]
    public int BatchSize;

    private string _databaseFilePath = null!;

    [GlobalSetup]
    public void Setup()
    {
        _databaseFilePath = Path.Combine(Path.GetTempPath(), $"bettersqlite-benchmark", $"batchsize-{BatchSize}-{Guid.NewGuid()}.db");
    }

    [IterationSetup]
    public void IterationSetup()
    {
        var batchingOptions = new BatchingOptions()
        {
            BatchSizeLimit = BatchSize,
            QueueLimit = null,
            BufferingTimeLimit = TimeSpan.FromDays(1),
            EagerlyEmitFirstEvent = false,
        };

        Log.Logger = new LoggerConfiguration()
            .WriteTo.SQLite(
                databaseFile: new(_databaseFilePath),
                batchingOptions: batchingOptions)
            .CreateLogger();
    }

    [Benchmark(Description = "Log 5k messages")]
    public async Task LogInformation()
    {
        for (var i = 0; i < LOGS_PER_ITER; i++)
        {
            Log.Logger.Information("User {User} logged in at {Now}", "user@user.com", DateTime.UtcNow);
        }

        await Log.CloseAndFlushAsync();
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        if (File.Exists(_databaseFilePath))
        {
            File.Delete(_databaseFilePath);
        }
    }
}

public class CustomConfig : ManualConfig
{
    public CustomConfig()
    {
        AddColumnProvider(DefaultColumnProviders.Instance);
        AddColumn(new LogsPerSecondColumn());
        AddLogger(ConsoleLogger.Unicode);
    }
}

public class Program
{
    public static void Main()
    {
        BenchmarkRunner.Run<BetterSqliteBenchmarks>(new CustomConfig());
    }
}
