using LoadTests;

if (args.Length == 0 ||
    !Enum.TryParse(
        args[0],
        ignoreCase: true,
        out LoadPattern pattern))
{
    Console.Error.WriteLine(
        "Usage: LoadTests <steady|burst> " +
        "[duration-seconds] " +
        "[events-per-second] " +
        "[repetitions]");

    return;
}

int durationSeconds =
    ParseArgument(args, 1, defaultValue: 10);

int eventsPerSecond =
    ParseArgument(args, 2, defaultValue: 100_000);

int repetitions =
    ParseArgument(args, 3, defaultValue: 5);

if (repetitions <= 0)
{
    Console.Error.WriteLine(
        "Repetitions must be greater than zero.");

    return;
}

var options = new LoadTestOptions
{
    Seed = 12345,
    DurationSeconds = durationSeconds,
    EventsPerSecond = eventsPerSecond,
    WindowMilliseconds = 100
};

Console.WriteLine("===== H9 LOAD TEST =====");
Console.WriteLine($"Pattern        : {pattern}");
Console.WriteLine($"Duration       : {options.DurationSeconds} s");
Console.WriteLine($"Average rate   : {options.EventsPerSecond:N0} logs/s");
Console.WriteLine($"Target per run : {options.TargetCount:N0}");
Console.WriteLine($"Repetitions    : {repetitions}");
Console.WriteLine();


var warmupOptions = new LoadTestOptions
{
    Seed = options.Seed,
    DurationSeconds = 1,
    EventsPerSecond = options.EventsPerSecond,
    WindowMilliseconds = options.WindowMilliseconds
};

Console.WriteLine("Warmup...");

_ = await BurstSteadyLoadTest.RunAsync(
    pattern,
    warmupOptions);

Console.WriteLine("Warmup completed.");
Console.WriteLine();

var results =
    new List<LoadTestResult>(
        capacity: repetitions);

for (int run = 1; run <= repetitions; run++)
{
    Console.WriteLine(
        $"Running {run}/{repetitions}...");

    LoadTestResult result =
        await BurstSteadyLoadTest.RunAsync(
            pattern,
            options);

    results.Add(result);

    PrintRun(run, result);
}

PrintSummary(pattern, results);

static int ParseArgument(
    string[] arguments,
    int index,
    int defaultValue)
{
    if (arguments.Length > index &&
        int.TryParse(
            arguments[index],
            out int parsed))
    {
        return parsed;
    }

    return defaultValue;
}

static void PrintRun(
    int run,
    LoadTestResult result)
{
    Console.WriteLine(
        $"Run {run,2} | " +
        $"Elapsed {result.ElapsedMilliseconds,10:N2} ms | " +
        $"CPU {result.CpuMilliseconds,9:N2} ms | " +
        $"Allocated " +
        $"{result.AllocatedBytes / 1024.0 / 1024.0,8:N2} MB");

    Console.WriteLine(
        $"       | " +
        $"GC {result.Gen0Collections}/" +
        $"{result.Gen1Collections}/" +
        $"{result.Gen2Collections} | " +
        $"Queue {result.MaximumQueueDepth,8:N0} | " +
        $"Avg {result.AverageLatencyMilliseconds,8:N3} ms | " +
        $"P95 {result.P95LatencyMilliseconds,8:N3} ms | " +
        $"P99 {result.P99LatencyMilliseconds,8:N3} ms | " +
        $"CV {result.ThroughputCoefficientOfVariation:N3}");

    Console.WriteLine(
        $"       | " +
        $"Processed {result.ProcessedCount:N0} | " +
        $"Checksum {result.Checksum}");

    Console.WriteLine();
}

static void PrintSummary(
    LoadPattern pattern,
    IReadOnlyList<LoadTestResult> results)
{
    Console.WriteLine("===== SUMMARY =====");
    Console.WriteLine($"Pattern              : {pattern}");
    Console.WriteLine($"Measured runs        : {results.Count}");

    PrintMetric(
        "Elapsed",
        results,
        result => result.ElapsedMilliseconds,
        "ms");

    PrintMetric(
        "CPU time",
        results,
        result => result.CpuMilliseconds,
        "ms");

    PrintMetric(
        "Throughput",
        results,
        result => result.ThroughputPerSecond,
        "logs/s");

    PrintMetric(
        "Allocated",
        results,
        result =>
            result.AllocatedBytes /
            1024.0 /
            1024.0,
        "MB");

    PrintMetric(
        "Gen0",
        results,
        result => result.Gen0Collections,
        string.Empty);

    PrintMetric(
        "Gen1",
        results,
        result => result.Gen1Collections,
        string.Empty);

    PrintMetric(
        "Gen2",
        results,
        result => result.Gen2Collections,
        string.Empty);

    PrintMetric(
        "Maximum queue",
        results,
        result => result.MaximumQueueDepth,
        "logs");

    PrintMetric(
        "Average latency",
        results,
        result =>
            result.AverageLatencyMilliseconds,
        "ms");

    PrintMetric(
        "P95 latency",
        results,
        result =>
            result.P95LatencyMilliseconds,
        "ms");

    PrintMetric(
        "P99 latency",
        results,
        result =>
            result.P99LatencyMilliseconds,
        "ms");

    PrintMetric(
        "Throughput CV",
        results,
        result =>
            result.ThroughputCoefficientOfVariation,
        string.Empty);

    bool checksumsMatch =
        results
            .Select(result => result.Checksum)
            .Distinct()
            .Count() == 1;

    bool countsMatch =
        results.All(
            result =>
                result.ProcessedCount ==
                result.TargetCount);

    Console.WriteLine(
        $"Checksums match       : {checksumsMatch}");

    Console.WriteLine(
        $"All counts match      : {countsMatch}");
}

static void PrintMetric(
    string name,
    IReadOnlyList<LoadTestResult> results,
    Func<LoadTestResult, double> selector,
    string unit)
{
    double[] values =
        results
            .Select(selector)
            .ToArray();

    double average =
        values.Average();

    double standardDeviation =
        CalculateStandardDeviation(
            values,
            average);

    Console.WriteLine(
        $"{name,-21}: " +
        $"{average:N3} {unit} " +
        $"(SD {standardDeviation:N3})");
}

static double CalculateStandardDeviation(
    IReadOnlyList<double> values,
    double average)
{
    if (values.Count <= 1)
    {
        return 0;
    }

    double squaredDifferenceSum = 0;

    foreach (double value in values)
    {
        double difference =
            value - average;

        squaredDifferenceSum +=
            difference * difference;
    }

    return Math.Sqrt(
        squaredDifferenceSum /
        (values.Count - 1));
}