using BenchmarkDotNet.Attributes;

namespace Benchmarks;

[MemoryDiagnoser]
public class AllocationReuseBenchmarks
{
    [Params(5_000, 100_000, 1_000_000)]
    public int TargetCount;

    private const int BufferSize = 1_024;

    private byte[] _reusableBuffer = null!;

    [GlobalSetup]
    public void Setup()
    {
        _reusableBuffer =
            new byte[BufferSize];
    }

    [Benchmark(Baseline = true)]
    public long AllocationHeavy()
    {
        long checksum = 0;

        for (int index = 0;
             index < TargetCount;
             index++)
        {
            byte[] buffer =
                new byte[BufferSize];

            Span<byte> span =
                buffer.AsSpan();

            byte value =
                (byte)index;

            span.Fill(value);

            checksum += span[0];
            checksum += span[BufferSize / 2];
            checksum += span[BufferSize - 1];
        }

        return checksum;
    }

    [Benchmark]
    public long ReuseHeavy()
    {
        long checksum = 0;

        byte[] buffer =
            _reusableBuffer;

        for (int index = 0;
             index < TargetCount;
             index++)
        {
            Span<byte> span =
                buffer.AsSpan();

            byte value =
                (byte)index;

            span.Fill(value);

            checksum += span[0];
            checksum += span[BufferSize / 2];
            checksum += span[BufferSize - 1];
        }

        return checksum;
    }
}