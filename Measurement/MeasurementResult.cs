namespace Measurement;

public class MeasurementResult
{
    public string Scenario { get; }

    public int Seed { get; }

    public int TargetCount { get; }

    public int ProducedCount { get; }

    public double ElapsedMilliseconds { get; }

    public long AllocatedBytes { get; }

    public long MemoryDifferenceBytes { get; }

    public int Gen0Collections { get; }

    public int Gen1Collections { get; }

    public int Gen2Collections { get; }

    public double ThroughputPerSecond { get; }

    public MeasurementResult(
        string scenario,
        int seed,
        int targetCount,
        int producedCount,
        double elapsedMilliseconds,
        long allocatedBytes,
        long memoryDifferenceBytes,
        int gen0Collections,
        int gen1Collections,
        int gen2Collections)
    {
        Scenario = scenario;
        Seed = seed;
        TargetCount = targetCount;
        ProducedCount = producedCount;
        ElapsedMilliseconds = elapsedMilliseconds;
        AllocatedBytes = allocatedBytes;
        MemoryDifferenceBytes = memoryDifferenceBytes;
        Gen0Collections = gen0Collections;
        Gen1Collections = gen1Collections;
        Gen2Collections = gen2Collections;
        ThroughputPerSecond = elapsedMilliseconds > 0
            ? producedCount / (elapsedMilliseconds / 1000.0)
            : 0;
    }
}