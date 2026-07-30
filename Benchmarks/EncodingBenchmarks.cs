using System.Text;
using BenchmarkDotNet.Attributes;
using Infrastructure;

namespace Benchmarks;

[MemoryDiagnoser]
public class EncodingBenchmarks
{
    [Params(5_000, 100_000, 1_000_000)] public int TargetCount;

    private byte[] _utf8Bytes = null!;
    private byte[] _utf16Bytes = null!;

    [GlobalSetup]
    public void Setup()
    {
        const string connectionStringVariable =
            "OPTIMIZATION_SQL_CONNECTION_STRING";

        const int messageSampleLength = 24;

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

        var builder =
            new StringBuilder(
                capacity: TargetCount * 64);

        LogEntryReadResult readResult =
            logRepository
                .ReadAsync(
                    datasetId,
                    log =>
                    {
                        int sampleLength =
                            Math.Min(
                                log.Message.Length,
                                messageSampleLength);

                        builder
                            .Append(log.Index)
                            .Append('|')
                            .Append(log.Level)
                            .Append('|')
                            .Append(log.From)
                            .Append('|')
                            .Append(log.To)
                            .Append('|')
                            .Append(
                                log.Message.AsSpan(
                                    0,
                                    sampleLength))
                            .Append('\n');
                    })
                .GetAwaiter()
                .GetResult();

        if (readResult.Count != TargetCount)
        {
            throw new InvalidOperationException(
                $"Expected {TargetCount:N0} rows, " +
                $"but loaded {readResult.Count:N0}.");
        }

        if (readResult.Checksum !=
            storedStatistics.Checksum)
        {
            throw new InvalidOperationException(
                "Loaded dataset checksum is invalid.");
        }

        string text =
            builder.ToString();

        _utf8Bytes =
            Encoding.UTF8.GetBytes(text);

        _utf16Bytes =
            Encoding.Unicode.GetBytes(text);

        string decodedUtf8 =
            Encoding.UTF8.GetString(_utf8Bytes);

        string decodedUtf16 =
            Encoding.Unicode.GetString(_utf16Bytes);

        if (!string.Equals(
                decodedUtf8,
                decodedUtf16,
                StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                "UTF-8 and UTF-16 datasets " +
                "do not decode to the same text.");
        }
    }

    [Benchmark(Baseline = true)]
    public string DecodeUtf8()
    {
        return
            Encoding.UTF8.GetString(_utf8Bytes);
    }

    [Benchmark]
    public string DecodeUtf16()
    {
        return
            Encoding.Unicode.GetString(
                _utf16Bytes);
    }
}