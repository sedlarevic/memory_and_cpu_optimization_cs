using Domain;

namespace Generator;

public class GeneratorEngine
{
    private readonly StateMachine _stateMachine;

    private readonly int _targetCount;

    private readonly ILogFactory _logFactory;

    private readonly Random _logRng;

    private int _producedLogs;

    public GeneratorEngine(
        Seed seed,
        int targetCount,
        ILogFactory logFactory)
    {
        ArgumentNullException.ThrowIfNull(seed);
        ArgumentNullException.ThrowIfNull(logFactory);

        if (targetCount <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(targetCount),
                "Target count must be greater than zero.");
        }
        Seed stateSeed = seed.Derive("state");
        Seed logSeed = seed.Derive("log");

        _stateMachine =
            new StateMachine(stateSeed);
        _logRng =
            new Random(logSeed.Value);
        _targetCount = targetCount;
        _logFactory = logFactory;
        _producedLogs = 0;
    }
    public int Run(Action<LogEntry> consumer)
    {
        ArgumentNullException.ThrowIfNull(consumer);
        while (_producedLogs < _targetCount)
        {
            (State from, State to) =
                _stateMachine.Step();
            int remaining =
                _targetCount - _producedLogs;
            int created = _logFactory.Create(
                from,
                to,
                _logRng,
                remaining,
                log =>
                {
                    _producedLogs++;
                    log.Index = _producedLogs;
                    consumer(log);
                });
            if (created <= 0)
            {
                throw new InvalidOperationException(
                    "Log factory did not produce any logs.");
            }
        }
        return _producedLogs;
    }
}