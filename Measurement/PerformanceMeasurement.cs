namespace Measurement;

using System.Diagnostics;

public class PerformanceMeasurement
{
    public static MeasurementResult Measure(
        string scenario,
        int seed,
        int targetCount,
        Func<int> workload)

    {
        ArgumentException.ThrowIfNullOrWhiteSpace(scenario);

        ArgumentNullException.ThrowIfNull(workload);

        if (targetCount <= 0)

        {
            throw new ArgumentOutOfRangeException(
                nameof(targetCount),
                "Target count must be greater than zero.");
        }

        // Svako kontrolisano merenje počinje iz približno

        // jednakog GC stanja.

        GC.Collect();

        GC.WaitForPendingFinalizers();

        GC.Collect();

        int gen0Before = GC.CollectionCount(0);

        int gen1Before = GC.CollectionCount(1);

        int gen2Before = GC.CollectionCount(2);

        long allocatedBefore =
            GC.GetTotalAllocatedBytes(precise: true);

        long memoryBefore =
            GC.GetTotalMemory(forceFullCollection: false);

        var stopwatch = Stopwatch.StartNew();

        int producedCount = workload();

        stopwatch.Stop();

        long memoryAfter =
            GC.GetTotalMemory(forceFullCollection: false);

        long allocatedAfter =
            GC.GetTotalAllocatedBytes(precise: true);

        int gen0After = GC.CollectionCount(0);

        int gen1After = GC.CollectionCount(1);

        int gen2After = GC.CollectionCount(2);

        return new MeasurementResult(
            scenario: scenario,
            seed: seed,
            targetCount: targetCount,
            producedCount: producedCount,
            elapsedMilliseconds: stopwatch.Elapsed.TotalMilliseconds,
            allocatedBytes: allocatedAfter - allocatedBefore,
            memoryDifferenceBytes: memoryAfter - memoryBefore,
            gen0Collections: gen0After - gen0Before,
            gen1Collections: gen1After - gen1Before,
            gen2Collections: gen2After - gen2Before);
    }
}