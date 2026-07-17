using System.Runtime.CompilerServices;
using BenchmarkDotNet.Attributes;

namespace Benchmarks;

[MemoryDiagnoser]
public class ExceptionHandlingBenchmarks
{
    private const int ValuesLength = 1024;

    private int[] _values = null!;

    [Params(5_000, 100_000, 1_000_000)] public int TargetCount { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        _values = new int[ValuesLength];

        for (int index = 0; index < _values.Length; index++)
        {
            _values[index] = index;
        }
    }

    [Benchmark]
    public int TryCatchAroundLoopNoThrow()
    {
        try
        {
            int checksum = 0;

            for (int iteration = 0;
                 iteration < TargetCount;
                 iteration++)
            {
                int index = iteration & (ValuesLength - 1);

                checksum = unchecked(
                    checksum +
                    ProcessWithoutTryCatch(_values, index));
            }

            return checksum;
        }
        catch (IndexOutOfRangeException)
        {
            return -1;
        }
    }

    [Benchmark(Baseline = true)]
    public int WithoutTryCatch()
    {
        int checksum = 0;

        for (int iteration = 0;
             iteration < TargetCount;
             iteration++)
        {
            int index = iteration & (ValuesLength - 1);

            checksum = unchecked(
                checksum +
                ProcessWithoutTryCatch(_values, index));
        }

        return checksum;
    }

    [Benchmark]
    public int TryCatchPerItemNoThrow()
    {
        int checksum = 0;

        for (int iteration = 0;
             iteration < TargetCount;
             iteration++)
        {
            int index = iteration & (ValuesLength - 1);

            checksum = unchecked(
                checksum +
                ProcessWithTryCatch(_values, index));
        }

        return checksum;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int ProcessWithoutTryCatch(
        int[] values,
        int index)
    {
        return values[index];
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int ProcessWithTryCatch(
        int[] values,
        int index)
    {
        try
        {
            return values[index];
        }
        catch (IndexOutOfRangeException)
        {
            return -1;
        }
    }
}