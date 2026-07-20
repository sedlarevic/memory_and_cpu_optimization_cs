using BenchmarkDotNet.Attributes;

namespace Benchmarks;

[MemoryDiagnoser]
public class LambdaBenchmarks
{
    private const int Offset = 17;

    private Func<int, int> _methodGroup = null!;
    private Func<int, int> _nonCapturingLambda = null!;
    private Func<int, int> _capturingLambda = null!;

    [Params(5_000, 100_000, 1_000_000)]
    public int TargetCount { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        _methodGroup = Transform;

        _nonCapturingLambda =
            static value => Transform(value);

        int capturedOffset = Offset;

        _capturingLambda =
            value => unchecked(
                value * 31 +
                capturedOffset);
    }

    [Benchmark(Baseline = true)]
    public long ExplicitMethod()
    {
        long checksum = 0;

        for (int index = 0;
             index < TargetCount;
             index++)
        {
            checksum += Transform(index);
        }

        return checksum;
    }

    [Benchmark]
    public long MethodGroupDelegate()
    {
        long checksum = 0;

        for (int index = 0;
             index < TargetCount;
             index++)
        {
            checksum += _methodGroup(index);
        }

        return checksum;
    }

    [Benchmark]
    public long NonCapturingLambda()
    {
        long checksum = 0;

        for (int index = 0;
             index < TargetCount;
             index++)
        {
            checksum += _nonCapturingLambda(index);
        }

        return checksum;
    }

    [Benchmark]
    public long CapturingLambda()
    {
        long checksum = 0;

        for (int index = 0;
             index < TargetCount;
             index++)
        {
            checksum += _capturingLambda(index);
        }

        return checksum;
    }

    private static int Transform(int value)
    {
        return unchecked(
            value * 31 +
            Offset);
    }
}