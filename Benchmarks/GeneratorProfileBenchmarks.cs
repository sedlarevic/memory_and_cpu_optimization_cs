using BenchmarkDotNet.Attributes;
using Generator;

namespace Benchmarks;

[MemoryDiagnoser]
public class GeneratorProfileBenchmarks
{
    [Params(5_000, 100_000, 1_000_000)] public int TargetCount;
    private const int SeedValue = 12345;

    [Benchmark(Baseline = true)]
    public int StandardProfile()
    {
        return RunGenerator(GenerationProfile.Standard);
    }

    [Benchmark]
    public int ErrorHeavyProfile()
    {
        return RunGenerator(GenerationProfile.ErrorHeavy);
    }

    private int RunGenerator(GenerationProfile profile)
    {
        Seed seed = new(SeedValue);
        ILogFactory factory =
            new LogFactory(profile);
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