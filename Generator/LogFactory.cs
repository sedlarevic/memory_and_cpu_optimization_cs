using Domain;

namespace Generator;

public class LogFactory : ILogFactory
{
    public IEnumerable<LogEntry> Create(State from, State to, Random rng)
    {
        if (to == State.Error && rng.NextDouble() < 0.3)
        {
            // burst happens here
        }
        else
        {
            // regular logging
        }
    }
}