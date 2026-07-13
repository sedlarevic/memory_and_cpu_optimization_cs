using BenchmarkDotNet.Attributes;
using Domain;
using Generator;

namespace Benchmarks;

[MemoryDiagnoser]
public class GeneratorBenchmarks
{
    [Params(5_000, 100_000, 1_000_000)] public int TargetCount;
    private const int SeedValue = 12345;

    [Benchmark(Baseline = true)]
    public int Steady()
    {
        return RunGenerator(GenerationMode.Steady);
    }

    [Benchmark]
    public int Burst()
    {
        return RunGenerator(GenerationMode.Burst);
    }

    private int RunGenerator(GenerationMode mode)
    {
        Seed seed = new(SeedValue);
        ILogFactory factory =
            new LogFactory(mode);
        GeneratorEngine engine =
            new GeneratorEngine(
                seed,
                TargetCount,
                factory);
        long checksum = 0;
        int producedCount = engine.Run(log => { checksum += log.Message.Length; });
        GC.KeepAlive(checksum);

        return producedCount;
    }
}