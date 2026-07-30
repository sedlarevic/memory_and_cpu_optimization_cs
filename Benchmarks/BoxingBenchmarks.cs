using BenchmarkDotNet.Attributes;
using Domain;
using Generator;
using Infrastructure;

namespace Benchmarks;

[MemoryDiagnoser]
public class BoxingBenchmarks
{
    [Params(5_000, 100_000, 1_000_000)] public int TargetCount;

    private const int SeedValue = 12345;

    private LogEntryValue[] _entries = null!;

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

        _entries =
            new LogEntryValue[TargetCount];

        int position = 0;

        LogEntryReadResult readResult =
            logRepository
                .ReadAsync(
                    datasetId,
                    log =>
                    {
                        if (position >= _entries.Length)
                        {
                            throw new InvalidOperationException(
                                "Dataset contains more rows " +
                                "than expected.");
                        }

                        _entries[position] =
                            new LogEntryValue(
                                log.Index,
                                log.From,
                                log.To,
                                log.Level,
                                log.Message);

                        position++;
                    })
                .GetAwaiter()
                .GetResult();

        if (position != TargetCount ||
            readResult.Count != TargetCount)
        {
            throw new InvalidOperationException(
                $"Expected {TargetCount:N0} entries, " +
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
    public long TypedList()
    {
        var entries =
            new List<LogEntryValue>(
                capacity: TargetCount);

        for (int index = 0;
             index < _entries.Length;
             index++)
        {
            entries.Add(_entries[index]);
        }

        long checksum = 0;

        for (int index = 0;
             index < entries.Count;
             index++)
        {
            LogEntryValue entry =
                entries[index];

            checksum += GetChecksum(entry);
        }

        return checksum;
    }

    [Benchmark]
    public long ObjectListBoxing()
    {
        var entries =
            new List<object>(
                capacity: TargetCount);

        for (int index = 0;
             index < _entries.Length;
             index++)
        {
            entries.Add(_entries[index]);
        }

        long checksum = 0;

        for (int index = 0;
             index < entries.Count;
             index++)
        {
            LogEntryValue entry =
                (LogEntryValue)entries[index];

            checksum += GetChecksum(entry);
        }

        return checksum;
    }

    private static long GetChecksum(
        LogEntryValue entry)
    {
        long checksum = entry.Index;

        checksum += (int)entry.From;
        checksum += (int)entry.To;
        checksum += entry.Level.Length;
        checksum += entry.Message.Length;

        return checksum;
    }
}