using BenchmarkDotNet.Attributes;
using Domain;

namespace Benchmarks;

[MemoryDiagnoser]
public class ClassStructGenerationBenchmarks
{
    [Params(5_000, 100_000, 1_000_000)]
    public int TargetCount;

    private const string Level = "[INFO]";

    private const string Message =
        "[INFO] RequestReceived -> Processing";

    private ClassLogRecord? _classSink;
    private StructLogRecord _structSink;

    [Benchmark(Baseline = true)]
    public long ClassRecord()
    {
        long checksum = 0;

        for (int index = 0; index < TargetCount; index++)
        {
            var log = new ClassLogRecord(
                index,
                State.RequestReceived,
                State.Processing,
                Level,
                Message);


            _classSink = log;

            checksum += log.Index;
            checksum += log.Message.Length;
        }

        return checksum;
    }

    [Benchmark]
    public long StructRecord()
    {
        long checksum = 0;

        for (int index = 0; index < TargetCount; index++)
        {
            var log = new StructLogRecord(
                index,
                State.RequestReceived,
                State.Processing,
                Level,
                Message);

            _structSink = log;

            checksum += log.Index;
            checksum += log.Message.Length;
        }

        return checksum;
    }

    private sealed class ClassLogRecord
    {
        public int Index { get; }
        public State From { get; }
        public State To { get; }
        public string Level { get; }
        public string Message { get; }

        public ClassLogRecord(
            int index,
            State from,
            State to,
            string level,
            string message)
        {
            Index = index;
            From = from;
            To = to;
            Level = level;
            Message = message;
        }
    }

    private readonly struct StructLogRecord
    {
        public int Index { get; }
        public State From { get; }
        public State To { get; }
        public string Level { get; }
        public string Message { get; }

        public StructLogRecord(
            int index,
            State from,
            State to,
            string level,
            string message)
        {
            Index = index;
            From = from;
            To = to;
            Level = level;
            Message = message;
        }
    }
}