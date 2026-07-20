namespace LoadTests;

public sealed class LoadTestResult
{
    public LoadPattern Pattern { get; init; }

    public int TargetCount { get; init; }

    public int ProcessedCount { get; init; }

    public double ElapsedMilliseconds { get; init; }

    public double CpuMilliseconds { get; init; }

    public double ThroughputPerSecond { get; init; }

    public long AllocatedBytes { get; init; }

    public int Gen0Collections { get; init; }

    public int Gen1Collections { get; init; }

    public int Gen2Collections { get; init; }

    public int MaximumQueueDepth { get; init; }

    public double AverageLatencyMilliseconds { get; init; }

    public double P95LatencyMilliseconds { get; init; }

    public double P99LatencyMilliseconds { get; init; }

    public double ThroughputCoefficientOfVariation { get; init; }

    public long Checksum { get; init; }
}