using Domain;

namespace Resolver;

public static class LogBatchResolver
{
    public static long ResolveSequential(string[] lines)
    {
        ArgumentNullException.ThrowIfNull(lines);

        long checksum = 0;

        for (int index = 0;
             index < lines.Length;
             index++)
        {
            checksum += ResolveLine(lines[index]);
        }

        return checksum;
    }

    public static long ResolveParallelFor(string[] lines)
    {
        ArgumentNullException.ThrowIfNull(lines);

        long checksum = 0;

        Parallel.For<long>(
            fromInclusive: 0,
            toExclusive: lines.Length,
            localInit: static () => 0L,
            body: (index, _, localChecksum) =>
            {
                return
                    localChecksum +
                    ResolveLine(lines[index]);
            },
            localFinally: localChecksum =>
            {
                Interlocked.Add(
                    ref checksum,
                    localChecksum);
            });

        return checksum;
    }

    public static long ResolvePlinq(string[] lines)
    {
        ArgumentNullException.ThrowIfNull(lines);

        return lines
            .AsParallel()
            .Sum(static line => ResolveLine(line));
    }

    private static long ResolveLine(string line)
    {
        LogEntry log =
            LogResolver.ResolveWithSpan(line);

        long checksum = log.Index;

        checksum += (int)log.From;
        checksum += (int)log.To;
        checksum += log.Level.Length;
        checksum += log.Message.Length;

        return checksum;
    }
}