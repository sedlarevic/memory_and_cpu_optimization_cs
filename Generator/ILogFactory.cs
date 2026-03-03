using Domain;

namespace Generator;

public interface ILogFactory
{
    IEnumerable<LogEntry> Create(State from, State to, Random rng);
}