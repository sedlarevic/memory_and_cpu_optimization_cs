using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using BenchmarkDotNet.Attributes;

namespace Benchmarks;

[StructLayout(LayoutKind.Sequential)]
public struct PoorlyAlignedRecord
{
    public byte FlagA;
    public long Timestamp;
    public byte FlagB;
    public int Code;
    public short Category;
}

[StructLayout(LayoutKind.Sequential)]
public struct OptimizedAlignedRecord
{
    public long Timestamp;
    public int Code;
    public short Category;
    public byte FlagA;
    public byte FlagB;
}


[MemoryDiagnoser]
public class StructAlignmentBenchmarks
{
    private PoorlyAlignedRecord[] _poorlyAligned = null!;
    private OptimizedAlignedRecord[] _optimized = null!;

    [Params(5_000, 100_000, 1_000_000)]
    public int TargetCount { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        int poorlyAlignedSize =
            Unsafe.SizeOf<PoorlyAlignedRecord>();

        int optimizedSize =
            Unsafe.SizeOf<OptimizedAlignedRecord>();

        if (poorlyAlignedSize <= optimizedSize)
        {
            throw new InvalidOperationException(
                "The poorly aligned struct must be larger.");
        }

        _poorlyAligned =
            new PoorlyAlignedRecord[TargetCount];

        _optimized =
            new OptimizedAlignedRecord[TargetCount];

        for (int index = 0;
             index < TargetCount;
             index++)
        {
            long timestamp = index;
            int code = index & 1023;
            short category = (short)(index & 127);
            byte flagA = (byte)(index & 1);
            byte flagB = (byte)((index >> 1) & 1);

            _poorlyAligned[index] =
                new PoorlyAlignedRecord
                {
                    Timestamp = timestamp,
                    Code = code,
                    Category = category,
                    FlagA = flagA,
                    FlagB = flagB
                };

            _optimized[index] =
                new OptimizedAlignedRecord
                {
                    Timestamp = timestamp,
                    Code = code,
                    Category = category,
                    FlagA = flagA,
                    FlagB = flagB
                };
        }
    }

    [Benchmark(Baseline = true)]
    public long PoorlyAligned()
    {
        long checksum = 0;

        for (int index = 0;
             index < _poorlyAligned.Length;
             index++)
        {
            PoorlyAlignedRecord record =
                _poorlyAligned[index];

            checksum = unchecked(
                checksum +
                record.Timestamp +
                record.Code +
                record.Category +
                record.FlagA +
                record.FlagB);
        }

        return checksum;
    }

    [Benchmark]
    public long OptimizedAlignment()
    {
        long checksum = 0;

        for (int index = 0;
             index < _optimized.Length;
             index++)
        {
            OptimizedAlignedRecord record =
                _optimized[index];

            checksum = unchecked(
                checksum +
                record.Timestamp +
                record.Code +
                record.Category +
                record.FlagA +
                record.FlagB);
        }

        return checksum;
    }
}