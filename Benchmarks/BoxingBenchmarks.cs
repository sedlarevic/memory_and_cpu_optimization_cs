using BenchmarkDotNet.Attributes;
using Domain;
using Generator;

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
        _entries =
            new LogEntryValue[TargetCount];

        Seed seed = new(SeedValue);

        ILogFactory factory =
            new LogFactory(GenerationMode.Steady);

        GeneratorEngine engine =
            new GeneratorEngine(
                seed,
                TargetCount,
                factory);

        int position = 0;

        engine.Run(log =>
        {
            _entries[position] =
                new LogEntryValue(
                    log.Index,
                    log.From,
                    log.To,
                    log.Level,
                    log.Message);

            position++;
        });

        if (position != TargetCount)
        {
            throw new InvalidOperationException(
                $"Expected {TargetCount} entries, " +
                $"but generated {position}.");
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
            // LogEntryValue se ovde boxuje.
            entries.Add(_entries[index]);
        }

        long checksum = 0;

        for (int index = 0;
             index < entries.Count;
             index++)
        {
            // Unboxing happens here.
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