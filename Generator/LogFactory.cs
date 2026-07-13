using Domain;

namespace Generator;

public class LogFactory : ILogFactory
{
    private readonly GenerationMode _mode;
    public LogFactory(GenerationMode mode)

    {
        _mode = mode;
    }

    public int Create(
        State from,
        State to,
        Random rng,
        int maxCount,
        Action<LogEntry> consumer)
    {
        ArgumentNullException.ThrowIfNull(rng);
        ArgumentNullException.ThrowIfNull(consumer);

        if (maxCount <= 0)
        {
            return 0;
        }

        int totalCount;
        bool burstOccurred = false;

        if (to == State.Error)
        {
            int baseCount = rng.Next(2, 6);
            
            if (_mode == GenerationMode.Burst)
            {
                double burstChance =
                    from is State.Processing or State.Retry
                        ? 0.40
                        : 0.20;
                burstOccurred =
                    rng.NextDouble() < burstChance;
            }
            int extraCount = burstOccurred
                ? rng.Next(20, 51)
                : 0;
            totalCount = baseCount + extraCount;
        }
        else if (
            from == State.Processing &&
            to == State.Completed)
        {
            totalCount = rng.Next(3, 9);
        }
        else if (
            from == State.Retry &&
            to == State.Processing)
        {
            totalCount = rng.Next(1, 4);
        }
        else
        {
            totalCount = 1;
        }
        
        totalCount = Math.Min(totalCount, maxCount);

        for (int i = 0; i < totalCount; i++)
        {
            string level;
            int payloadLength;

            if (to == State.Error)
            {
                level =
                    i == 0
                        ? "[ERROR]"
                        : "[WARNING]";

                payloadLength = burstOccurred
                    ? rng.Next(800, 2001)
                    : rng.Next(200, 601);
            }
            else if (
                from == State.Processing &&
                to == State.Completed)
            {
                level = "[INFO]";
                payloadLength = rng.Next(120, 301);
            }
            else if (
                from == State.Retry &&
                to == State.Processing)
            {
                level = "[WARNING]";
                payloadLength = rng.Next(60, 141);
            }
            else
            {
                level = "[INFO]";
                payloadLength = rng.Next(40, 121);
            }

            string payload =
                new string('X', payloadLength);
            string message =
                $"{level} {from} -> {to} | {payload}";

            var log = new LogEntry(
                index: 0,
                from,
                to,
                level,
                message);
            consumer(log);
        }
        return totalCount;
    }
}