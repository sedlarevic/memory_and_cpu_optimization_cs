namespace Measurement;

public static class ExperimentHarness
{
    public static ExperimentSummary Run(
        string scenario,
        int seed,
        int targetCount,
        Func<int> workload,
        int measurementRuns = 10,
        int discardFastest = 2,
        int discardSlowest = 2,
        bool runWarmup = true)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(scenario);
        ArgumentNullException.ThrowIfNull(workload);

        if (measurementRuns <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(measurementRuns),
                "Measurement run count must be greater than zero.");
        }

        if (discardFastest < 0 || discardSlowest < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(discardFastest),
                "Discard counts cannot be negative.");
        }

        if (discardFastest + discardSlowest >= measurementRuns)
        {
            throw new ArgumentException(
                "At least one result must remain after discarding.");
        }

        if (runWarmup)
        {
            _ = workload();
        }
        var results = new List<MeasurementResult>(
            capacity: measurementRuns);
        for (int run = 0; run < measurementRuns; run++)
        {
            MeasurementResult result =
                PerformanceMeasurement.Measure(
                    scenario,
                    seed,
                    targetCount,
                    workload);
            results.Add(result);
        }

        List<MeasurementResult> includedResults = results
            .OrderBy(result => result.ElapsedMilliseconds)
            .Skip(discardFastest)
            .Take(
                measurementRuns -
                discardFastest -
                discardSlowest)
            .ToList();

        return new ExperimentSummary(
            scenario,
            results,
            includedResults);
    }
}