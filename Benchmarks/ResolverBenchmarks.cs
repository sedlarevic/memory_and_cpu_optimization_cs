using BenchmarkDotNet.Attributes;
using Domain;
using Generator;
using Resolver;

namespace Benchmarks;

[MemoryDiagnoser]
public class ResolverBenchmarks
{
    [Params(5_000, 100_000, 1_000_000)]
    public int TargetCount;

    private const int SeedValue = 12345;

    private string[] _lines = null!;

    [GlobalSetup]
    public void Setup()
    {
        _lines = new string[TargetCount];

        Seed seed = new(SeedValue);

        ILogFactory factory =
            new LogFactory(GenerationProfile.Standard);

        GeneratorEngine engine =
            new GeneratorEngine(
                seed,
                TargetCount,
                factory);

        int position = 0;

        engine.Run(log =>
        {
            _lines[position] =
                LogLineFormat.Format(log);

            position++;
        });

        if (position != TargetCount)
        {
            throw new InvalidOperationException(
                $"Expected {TargetCount} lines, " +
                $"but generated {position}.");
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