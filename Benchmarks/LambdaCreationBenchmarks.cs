using BenchmarkDotNet.Attributes;

namespace Benchmarks;

[MemoryDiagnoser]
public class LambdaCreationBenchmarks
{
    private int _offset = 17;

    [Benchmark(Baseline = true)]
    public Func<int, int> StaticMethodGroupCreation()
    {
        return Transform;
    }

    [Benchmark]
    public Func<int, int> NonCapturingLambdaCreation()
    {
        return static value =>
            Transform(value);
    }

    [Benchmark]
    public Func<int, int> CapturingLambdaCreation()
    {
        int capturedOffset =
            unchecked(_offset++);

        return value =>
            unchecked(
                value * 31 +
                capturedOffset);
    }

    private static int Transform(int value)
    {
        return unchecked(
            value * 31 +
            17);
    }
}