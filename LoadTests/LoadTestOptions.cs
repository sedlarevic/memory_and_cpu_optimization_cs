namespace LoadTests;

public sealed class LoadTestOptions
{
    public int Seed { get; init; } = 12345;

    public int DurationSeconds { get; init; } = 10;

    public int EventsPerSecond { get; init; } = 100_000;

    public int WindowMilliseconds { get; init; } = 100;

    public int TargetCount =>
        DurationSeconds * EventsPerSecond;
}