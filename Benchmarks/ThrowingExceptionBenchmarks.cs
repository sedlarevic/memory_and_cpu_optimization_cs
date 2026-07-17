using System.Runtime.CompilerServices;
using BenchmarkDotNet.Attributes;

namespace Benchmarks;

[MemoryDiagnoser]
public class ThrowingExceptionBenchmarks
{
    private const int ValuesLength = 1024;

    private int[] _values = null!;

    [Params(10_000, 100_000)]
    public int TargetCount { get; set; }

    [Params(1000, 100)]
    public int ErrorEvery { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        _values = new int[ValuesLength];

        for (int index = 0; index < _values.Length; index++)
        {
            _values[index] = index;
        }
    }

    [Benchmark(Baseline = true)]
    public int ReturnErrorCode()
    {
        int checksum = 0;

        for (int iteration = 0;
             iteration < TargetCount;
             iteration++)
        {
            int index = iteration & (ValuesLength - 1);
            bool shouldFail = iteration % ErrorEvery == 0;

            int result = ReadWithErrorCode(
                _values,
                index,
                shouldFail);

            if (result >= 0)
            {
                checksum = unchecked(checksum + result);
            }
            else
            {
                checksum--;
            }
        }

        return checksum;
    }

    [Benchmark]
    public int ThrowAndCatch()
    {
        int checksum = 0;

        for (int iteration = 0;
             iteration < TargetCount;
             iteration++)
        {
            int index = iteration & (ValuesLength - 1);
            bool shouldFail = iteration % ErrorEvery == 0;

            try
            {
                checksum = unchecked(
                    checksum +
                    ReadOrThrow(
                        _values,
                        index,
                        shouldFail));
            }
            catch (InvalidOperationException)
            {
                checksum--;
            }
        }

        return checksum;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int ReadWithErrorCode(
        int[] values,
        int index,
        bool shouldFail)
    {
        if (shouldFail)
        {
            return -1;
        }

        return values[index];
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int ReadOrThrow(
        int[] values,
        int index,
        bool shouldFail)
    {
        if (shouldFail)
        {
            throw new InvalidOperationException(
                "Controlled benchmark exception.");
        }

        return values[index];
    }
}