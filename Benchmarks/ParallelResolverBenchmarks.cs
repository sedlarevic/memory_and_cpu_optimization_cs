using BenchmarkDotNet.Attributes;
using Domain;
using Generator;
using Resolver;

namespace Benchmarks;

[SimpleJob(
    launchCount: 3,
    warmupCount: 5,
    iterationCount: 15)]
[ThreadingDiagnoser]
public class ParallelResolverBenchmarks
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
            new LogFactory(GenerationMode.Steady);

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
    public long Sequential()
    {
        return
            LogBatchResolver.ResolveSequential(_lines);
    }

    [Benchmark]
    public long ParallelFor()
    {
        return
            LogBatchResolver.ResolveParallelFor(_lines);
    }

    [Benchmark]
    public long Plinq()
    {
        return
            LogBatchResolver.ResolvePlinq(_lines);
    }
}