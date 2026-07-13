namespace Measurement;

public sealed class ExperimentSummary
{
    public string Scenario { get; }
    public int TotalRuns { get; }
    public int IncludedRuns { get; }
    public double AverageElapsedMilliseconds { get; }
    public double AverageThroughputPerSecond { get; }
    public double AverageAllocatedBytes { get; }
    public double AverageMemoryDifferenceBytes { get; }
    public double AverageGen0Collections { get; }
    public double AverageGen1Collections { get; }
    public double AverageGen2Collections { get; }
    
    public IReadOnlyList<MeasurementResult> AllResults { get; }
    
    public IReadOnlyList<MeasurementResult> IncludedResults { get; }

    public ExperimentSummary(
        string scenario,
        IReadOnlyList<MeasurementResult> allResults,
        IReadOnlyList<MeasurementResult> includedResults)

    {
        Scenario = scenario;
        AllResults = allResults;
        IncludedResults = includedResults;
        TotalRuns = allResults.Count;
        IncludedRuns = includedResults.Count;

        AverageElapsedMilliseconds =
            includedResults.Average(result => result.ElapsedMilliseconds);
        AverageThroughputPerSecond =
            includedResults.Average(result => result.ThroughputPerSecond);
        AverageAllocatedBytes =
            includedResults.Average(result => result.AllocatedBytes);
        AverageMemoryDifferenceBytes =
            includedResults.Average(result => result.MemoryDifferenceBytes);
        AverageGen0Collections =
            includedResults.Average(result => result.Gen0Collections);
        AverageGen1Collections =
            includedResults.Average(result => result.Gen1Collections);
        AverageGen2Collections =
            includedResults.Average(result => result.Gen2Collections);
    }
}