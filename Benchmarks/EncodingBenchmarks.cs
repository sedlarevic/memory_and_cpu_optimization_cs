using System.Text;
using BenchmarkDotNet.Attributes;

namespace Benchmarks;

[MemoryDiagnoser]
public class EncodingBenchmarks
{
    [Params(5_000, 100_000, 1_000_000)]
    public int TargetCount;

    private byte[] _utf8Bytes = null!;
    private byte[] _utf16Bytes = null!;

    [GlobalSetup]
    public void Setup()
    {
        var builder =
            new StringBuilder(
                capacity: TargetCount * 64);

        for (int index = 1;
             index <= TargetCount;
             index++)
        {
            builder
                .Append(index)
                .Append("|[INFO]|Idle|Processing|")
                .Append("[INFO] Idle -> Processing")
                .Append('\n');
        }

        string text = builder.ToString();

        _utf8Bytes =
            Encoding.UTF8.GetBytes(text);

        _utf16Bytes =
            Encoding.Unicode.GetBytes(text);

        string decodedUtf8 =
            Encoding.UTF8.GetString(_utf8Bytes);

        string decodedUtf16 =
            Encoding.Unicode.GetString(_utf16Bytes);

        if (!string.Equals(
                decodedUtf8,
                decodedUtf16,
                StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                "UTF-8 and UTF-16 datasets " +
                "do not decode to the same text.");
        }
    }

    [Benchmark(Baseline = true)]
    public string DecodeUtf8()
    {
        return
            Encoding.UTF8.GetString(_utf8Bytes);
    }

    [Benchmark]
    public string DecodeUtf16()
    {
        return
            Encoding.Unicode.GetString(
                _utf16Bytes);
    }
}