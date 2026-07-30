using BenchmarkDotNet.Attributes;
using Domain;
using Generator;
using Infrastructure;
using Resolver;

namespace Benchmarks;

[MemoryDiagnoser]
public class ResolverBenchmarks
{
    [Params(5_000, 100_000, 1_000_000)] public int TargetCount;

    private const int SeedValue = 12345;

    private string[] _lines = null!;

    [GlobalSetup]
    public void Setup()
    {
        const string connectionStringVariable =
            "OPTIMIZATION_SQL_CONNECTION_STRING";

        string? connectionString =
            Environment.GetEnvironmentVariable(
                connectionStringVariable);

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                $"Environment variable " +
                $"'{connectionStringVariable}' is not configured.");
        }

        string datasetName =
            TargetCount switch
            {
                5_000 =>
                    "standard-5k-12345",

                100_000 =>
                    "standard-100k-12345",

                1_000_000 =>
                    "standard-1m-12345",

                _ => throw new InvalidOperationException(
                    $"No controlled dataset exists " +
                    $"for {TargetCount:N0} rows.")
            };

        var database =
            new SqlDatabase(connectionString);

        var datasetRepository =
            new DatasetRepository(database);

        var logRepository =
            new LogEntryRepository(database);

        long datasetId =
            datasetRepository
                .GetIdByNameAsync(datasetName)
                .GetAwaiter()
                .GetResult();

        LogEntryStatistics storedStatistics =
            logRepository
                .GetStatisticsAsync(datasetId)
                .GetAwaiter()
                .GetResult();

        if (storedStatistics.Count != TargetCount)
        {
            throw new InvalidOperationException(
                $"Dataset '{datasetName}' contains " +
                $"{storedStatistics.Count:N0} rows, " +
                $"but {TargetCount:N0} were expected.");
        }

        _lines = new string[TargetCount];

        int position = 0;

        LogEntryReadResult readResult =
            logRepository
                .ReadAsync(
                    datasetId,
                    log =>
                    {
                        if (position >= _lines.Length)
                        {
                            throw new InvalidOperationException(
                                "Dataset contains more rows " +
                                "than expected.");
                        }

                        _lines[position] =
                            LogLineFormat.Format(log);

                        position++;
                    })
                .GetAwaiter()
                .GetResult();

        if (position != TargetCount ||
            readResult.Count != TargetCount)
        {
            throw new InvalidOperationException(
                $"Expected {TargetCount:N0} lines, " +
                $"but loaded {position:N0}.");
        }

        if (readResult.Checksum !=
            storedStatistics.Checksum)
        {
            throw new InvalidOperationException(
                "Loaded dataset checksum is invalid.");
        }
    }

    [Benchmark(Baseline = true)]
    public long Split()
    {
        long checksum = 0;

        foreach (string line in _lines)
        {
            LogEntry log =
                LogResolver.ResolveWithSplit(line);

            checksum += log.Index;
            checksum += (int)log.From;
            checksum += (int)log.To;
            checksum += log.Level.Length;
            checksum += log.Message.Length;
        }

        return checksum;
    }

    [Benchmark]
    public long Span()
    {
        long checksum = 0;

        foreach (string line in _lines)
        {
            LogEntry log =
                LogResolver.ResolveWithSpan(line);

            checksum += log.Index;
            checksum += (int)log.From;
            checksum += (int)log.To;
            checksum += log.Level.Length;
            checksum += log.Message.Length;
        }

        return checksum;
    }
}