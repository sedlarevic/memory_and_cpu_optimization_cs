using System.Runtime.CompilerServices;
using BenchmarkDotNet.Attributes;

namespace Benchmarks;
[MemoryDiagnoser]
public class StackHeapBenchmarks
{
    private const int BufferSize = 256;

    [Params(5_000, 100_000, 1_000_000)]
    public int TargetCount;

    [Benchmark(Baseline = true)]
    public long HeapArray()
    {
        long checksum = 0;

        for (int index = 0;
             index < TargetCount;
             index++)
        {
            checksum += ProcessWithHeap(index);
        }

        return checksum;
    }

    [Benchmark]
    public long StackAlloc()
    {
        long checksum = 0;

        for (int index = 0;
             index < TargetCount;
             index++)
        {
            checksum += ProcessWithStack(index);
        }

        return checksum;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int ProcessWithHeap(int value)
    {
        byte[] buffer =
            new byte[BufferSize];

        buffer.AsSpan().Fill((byte)value);

        return ReadBuffer(buffer);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int ProcessWithStack(int value)
    {
        Span<byte> buffer =
            stackalloc byte[BufferSize];

        buffer.Fill((byte)value);

        return ReadBuffer(buffer);
    }

    private static int ReadBuffer(
        ReadOnlySpan<byte> buffer)
    {
        return
            buffer[0] +
            buffer[BufferSize / 2] +
            buffer[BufferSize - 1];
    }
}