using Domain;

namespace Generator;

public class GeneratorEngine
{
    private readonly StateMachine _stateMachine;
    private readonly int _targetCount;
    private readonly ILogFactory _logFactory;
    private readonly Random _logRng;
    private int _producedLogs;
    public GeneratorEngine(Seed seed, int targetCount, ILogFactory logFactory)
    {
        if (targetCount <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(targetCount),
                "Target count must be greater than zero.");
        }
        var stateSeed = seed.Derive("state");
        var logSeed = seed.Derive("log");
        _stateMachine = new StateMachine(stateSeed);
        _logRng = new Random(logSeed.Value);
        _targetCount = targetCount;
        _logFactory = logFactory;
        _producedLogs = 0;
    }

    public IEnumerable<LogEntry> Run()

    {

        while (_producedLogs < _targetCount)

        {
            var transition = _stateMachine.Step();
            IEnumerable<LogEntry> logs = _logFactory.Create(
                transition.From,
                transition.To,
                _logRng);
            foreach (LogEntry log in logs)
            {
                if (_producedLogs >= _targetCount)
                {
                    yield break;
                }
                _producedLogs++;
                log.Index = _producedLogs;
                yield return log;
            }
        }
    }

}