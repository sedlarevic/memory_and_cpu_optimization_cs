using Domain;

namespace Generator;

public interface ILogFactory
{
    int Create(
        State from,
        State to,
        Random rng,
        int maxCount,
        Action<LogEntry> consumer);
}