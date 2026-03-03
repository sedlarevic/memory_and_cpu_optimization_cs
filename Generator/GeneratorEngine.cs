using Domain;

namespace Generator;

public class GeneratorEngine
{
    private readonly StateMachine _stateMachine;
    private readonly int _targetCount;
    private int _producedLogs;
    private readonly ILogFactory _logFactory;
    private readonly Random _logRng;
    public GeneratorEngine(Seed seed, int targetCount, ILogFactory logFactory)
    {
        var stateSeed = seed.Derive("state");
        var logSeed =  seed.Derive("log"); 
        
        _stateMachine = new StateMachine(stateSeed);
        _targetCount =  targetCount;
        _logRng = new Random(logSeed.Value);
        _producedLogs = 0;
        
        _logFactory = logFactory;
    }

    public IEnumerable<LogEntry> Run()
    {
        while (_producedLogs < _targetCount)
        {
            ValueTuple<State, State> transition = _stateMachine.Step();
            foreach (var log in _logFactory.Create(transition.Item1, transition.Item2,_logRng))
            {
                if (_producedLogs >= _targetCount)
                    yield break;
                _producedLogs++;
                yield return log;
            }    
        }
    }

}