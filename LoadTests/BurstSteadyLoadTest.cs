using System.Diagnostics;
using System.Threading.Channels;
using Domain;
using Generator;

namespace LoadTests;

public static class BurstSteadyLoadTest
{
    public static async Task<LoadTestResult> RunAsync(
        LoadPattern pattern,
        LoadTestOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        Validate(options);

        int windowsPerSecond =
            1_000 / options.WindowMilliseconds;

        int totalWindows =
            options.DurationSeconds * windowsPerSecond;

        int targetCount = options.TargetCount;

        int batchSize =
            pattern == LoadPattern.Steady
                ? options.EventsPerSecond / windowsPerSecond
                : options.EventsPerSecond;

        int intervalMilliseconds =
            pattern == LoadPattern.Steady
                ? options.WindowMilliseconds
                : 1_000;

        long intervalTicks =
            intervalMilliseconds *
            Stopwatch.Frequency /
            1_000L;

        var latencyTicks =
            new long[targetCount];

        var completedPerWindow =
            new int[totalWindows];

        Channel<TimedLog> channel =
            Channel.CreateUnbounded<TimedLog>(
                new UnboundedChannelOptions
                {
                    SingleReader = true,
                    SingleWriter = true,
                    AllowSynchronousContinuations = false
                });

        int currentQueueDepth = 0;
        int maximumQueueDepth = 0;
        int processedCount = 0;

        long checksum = 0;

        // Svako izvršavanje počinje iz približno istog GC stanja.
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        long allocatedBefore =
            GC.GetTotalAllocatedBytes(precise: true);

        int gen0Before = GC.CollectionCount(0);
        int gen1Before = GC.CollectionCount(1);
        int gen2Before = GC.CollectionCount(2);

        using Process process =
            Process.GetCurrentProcess();

        process.Refresh();

        TimeSpan cpuBefore =
            process.TotalProcessorTime;

        long experimentStarted =
            Stopwatch.GetTimestamp();

        Task consumerTask = Task.Run(
            async () =>
            {
                long localChecksum = 0;

                await foreach (
                    TimedLog item in
                    channel.Reader.ReadAllAsync())
                {
                    Interlocked.Decrement(
                        ref currentQueueDepth);

                    LogEntry log = item.Log;

                    // Simulira jednostavnu obradu zapisa.
                    // Identična je u oba scenarija.
                    for (int index = 0;
                         index < log.Message.Length;
                         index++)
                    {
                        localChecksum = unchecked(
                            localChecksum * 31 +
                            log.Message[index]);
                    }

                    long completedTimestamp =
                        Stopwatch.GetTimestamp();

                    long latency =
                        completedTimestamp -
                        item.EnqueuedTimestamp;

                    latencyTicks[log.Index - 1] =
                        latency;

                    long elapsedTicks =
                        completedTimestamp -
                        experimentStarted;

                    long elapsedMilliseconds =
                        elapsedTicks *
                        1_000L /
                        Stopwatch.Frequency;

                    int windowIndex =
                        (int)(
                            elapsedMilliseconds /
                            options.WindowMilliseconds);

                    // Ako potrošač završi posle planiranog
                    // trajanja, rezultat pripada poslednjem prozoru.
                    windowIndex = Math.Min(
                        windowIndex,
                        totalWindows - 1);

                    completedPerWindow[windowIndex]++;

                    processedCount++;
                }

                checksum = localChecksum;
            });

        Task producerTask = Task.Run(
            () =>
            {
                Exception? failure = null;

                try
                {
                    Seed seed =
                        new(options.Seed);

                    // Sadržaj je identičan za oba H9 scenarija.
                    ILogFactory factory =
                        new LogFactory(
                            GenerationMode.Steady);

                    GeneratorEngine engine =
                        new GeneratorEngine(
                            seed,
                            targetCount,
                            factory);

                    int emittedCount = 0;

                    long nextDeadline =
                        experimentStarted +
                        intervalTicks;

                    int producedCount = engine.Run(
                        log =>
                        {
                            var timedLog =
                                new TimedLog(
                                    log,
                                    Stopwatch.GetTimestamp());

                            int queueDepth =
                                Interlocked.Increment(
                                    ref currentQueueDepth);

                            UpdateMaximum(
                                ref maximumQueueDepth,
                                queueDepth);

                            if (!channel.Writer.TryWrite(timedLog))
                            {
                                Interlocked.Decrement(
                                    ref currentQueueDepth);

                                throw new InvalidOperationException(
                                    "Could not write a log to the channel.");
                            }

                            emittedCount++;

                            if (emittedCount % batchSize == 0)
                            {
                                WaitUntil(nextDeadline);

                                nextDeadline +=
                                    intervalTicks;
                            }
                        });

                    if (producedCount != targetCount)
                    {
                        throw new InvalidOperationException(
                            $"Expected {targetCount:N0} logs, " +
                            $"but produced {producedCount:N0}.");
                    }
                }
                catch (Exception exception)
                {
                    failure = exception;
                    throw;
                }
                finally
                {
                    channel.Writer.TryComplete(failure);
                }
            });

        await Task.WhenAll(
            producerTask,
            consumerTask);

        long experimentFinished =
            Stopwatch.GetTimestamp();

        process.Refresh();

        TimeSpan cpuAfter =
            process.TotalProcessorTime;

        long allocatedAfter =
            GC.GetTotalAllocatedBytes(precise: true);

        int gen0After = GC.CollectionCount(0);
        int gen1After = GC.CollectionCount(1);
        int gen2After = GC.CollectionCount(2);

        double elapsedMilliseconds =
            TicksToMilliseconds(
                experimentFinished -
                experimentStarted);

        // Statistika se računa nakon merenog dela.
        // Sortiranje latencija zato ne utiče na rezultate testa.
        Array.Sort(latencyTicks);

        double averageLatencyMilliseconds =
            CalculateAverageLatency(latencyTicks);

        double p95LatencyMilliseconds =
            CalculatePercentile(
                latencyTicks,
                percentile: 0.95);

        double p99LatencyMilliseconds =
            CalculatePercentile(
                latencyTicks,
                percentile: 0.99);

        double throughputVariation =
            CalculateCoefficientOfVariation(
                completedPerWindow);

        GC.KeepAlive(checksum);

        return new LoadTestResult
        {
            Pattern = pattern,
            TargetCount = targetCount,
            ProcessedCount = processedCount,

            ElapsedMilliseconds =
                elapsedMilliseconds,

            CpuMilliseconds =
                (cpuAfter - cpuBefore)
                .TotalMilliseconds,

            ThroughputPerSecond =
                processedCount /
                (elapsedMilliseconds / 1_000.0),

            AllocatedBytes =
                allocatedAfter - allocatedBefore,

            Gen0Collections =
                gen0After - gen0Before,

            Gen1Collections =
                gen1After - gen1Before,

            Gen2Collections =
                gen2After - gen2Before,

            MaximumQueueDepth =
                maximumQueueDepth,

            AverageLatencyMilliseconds =
                averageLatencyMilliseconds,

            P95LatencyMilliseconds =
                p95LatencyMilliseconds,

            P99LatencyMilliseconds =
                p99LatencyMilliseconds,

            ThroughputCoefficientOfVariation =
                throughputVariation,

            Checksum = checksum
        };
    }

    private static void Validate(
        LoadTestOptions options)
    {
        if (options.DurationSeconds <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(options.DurationSeconds));
        }

        if (options.EventsPerSecond <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(options.EventsPerSecond));
        }

        if (options.WindowMilliseconds <= 0 ||
            1_000 % options.WindowMilliseconds != 0)
        {
            throw new ArgumentException(
                "Window size must divide one second exactly.");
        }

        int windowsPerSecond =
            1_000 / options.WindowMilliseconds;

        if (options.EventsPerSecond %
            windowsPerSecond != 0)
        {
            throw new ArgumentException(
                "Events per second must be divisible " +
                "by the number of windows per second.");
        }
    }

    private static void WaitUntil(
        long deadlineTimestamp)
    {
        while (true)
        {
            long remainingTicks =
                deadlineTimestamp -
                Stopwatch.GetTimestamp();

            if (remainingTicks <= 0)
            {
                return;
            }

            double remainingMilliseconds =
                remainingTicks *
                1_000.0 /
                Stopwatch.Frequency;

            // Koristimo čekanje umesto aktivnog spinovanja,
            // da sam generator opterećenja ne troši CPU.
            int sleepMilliseconds =
                Math.Max(
                    1,
                    (int)Math.Ceiling(
                        remainingMilliseconds));

            Thread.Sleep(sleepMilliseconds);
        }
    }

    private static void UpdateMaximum(
        ref int maximum,
        int candidate)
    {
        int current =
            Volatile.Read(ref maximum);

        while (candidate > current)
        {
            int observed =
                Interlocked.CompareExchange(
                    ref maximum,
                    candidate,
                    current);

            if (observed == current)
            {
                return;
            }

            current = observed;
        }
    }

    private static double CalculateAverageLatency(
        long[] values)
    {
        double sum = 0;

        foreach (long value in values)
        {
            sum += value;
        }

        return TicksToMilliseconds(
            sum / values.Length);
    }

    private static double CalculatePercentile(
        long[] sortedValues,
        double percentile)
    {
        int index =
            (int)Math.Ceiling(
                percentile *
                sortedValues.Length) - 1;

        index = Math.Clamp(
            index,
            0,
            sortedValues.Length - 1);

        return TicksToMilliseconds(
            sortedValues[index]);
    }

    private static double
        CalculateCoefficientOfVariation(
            int[] values)
    {
        double average =
            values.Average();

        if (average == 0)
        {
            return 0;
        }

        double squaredDifferenceSum = 0;

        foreach (int value in values)
        {
            double difference =
                value - average;

            squaredDifferenceSum +=
                difference * difference;
        }

        double standardDeviation =
            Math.Sqrt(
                squaredDifferenceSum /
                values.Length);

        return standardDeviation / average;
    }

    private static double TicksToMilliseconds(
        double ticks)
    {
        return
            ticks *
            1_000.0 /
            Stopwatch.Frequency;
    }

    private readonly struct TimedLog
    {
        public LogEntry Log { get; }

        public long EnqueuedTimestamp { get; }

        public TimedLog(
            LogEntry log,
            long enqueuedTimestamp)
        {
            Log = log;
            EnqueuedTimestamp =
                enqueuedTimestamp;
        }
    }
}