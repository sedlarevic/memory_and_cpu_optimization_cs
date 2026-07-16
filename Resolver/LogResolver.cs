using Domain;

namespace Resolver;

public class LogResolver
{
    public static LogEntry ResolveWithSplit(string line)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(line);

        string[] parts =
            line.Split(
                '|',
                5,
                StringSplitOptions.None);

        if (parts.Length != 5)
        {
            throw new FormatException(
                "Log line must contain five fields.");
        }

        if (!int.TryParse(parts[0], out int index) ||
            index <= 0)
        {
            throw new FormatException(
                "Log index must be a positive integer.");
        }

        string level = parts[1];

        if (!IsValidLevel(level))
        {
            throw new FormatException(
                $"Unknown log level: '{level}'.");
        }

        if (!Enum.TryParse(
                parts[2],
                ignoreCase: false,
                out State from))
        {
            throw new FormatException(
                $"Unknown source state: '{parts[2]}'.");
        }

        if (!Enum.TryParse(
                parts[3],
                ignoreCase: false,
                out State to))
        {
            throw new FormatException(
                $"Unknown destination state: '{parts[3]}'.");
        }

        string message = parts[4];

        if (string.IsNullOrWhiteSpace(message))
        {
            throw new FormatException(
                "Log message cannot be empty.");
        }

        return new LogEntry(
            index,
            from,
            to,
            level,
            message);
    }
    public static LogEntry ResolveWithSpan(string line)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(line);

        ReadOnlySpan<char> remaining = line.AsSpan();

        ReadOnlySpan<char> indexSpan =
            ReadNextField(ref remaining);

        ReadOnlySpan<char> levelSpan =
            ReadNextField(ref remaining);

        ReadOnlySpan<char> fromSpan =
            ReadNextField(ref remaining);

        ReadOnlySpan<char> toSpan =
            ReadNextField(ref remaining);

        // Sve nakon četvrtog separatora pripada poruci.
        ReadOnlySpan<char> messageSpan = remaining;

        if (!int.TryParse(indexSpan, out int index) ||
            index <= 0)
        {
            throw new FormatException(
                "Log index must be a positive integer.");
        }

        if (!IsValidLevel(levelSpan))
        {
            throw new FormatException(
                $"Unknown log level: '{levelSpan.ToString()}'.");
        }

        if (!Enum.TryParse(
                fromSpan,
                ignoreCase: false,
                out State from))
        {
            throw new FormatException(
                $"Unknown source state: '{fromSpan.ToString()}'.");
        }

        if (!Enum.TryParse(
                toSpan,
                ignoreCase: false,
                out State to))
        {
            throw new FormatException(
                $"Unknown destination state: '{toSpan.ToString()}'.");
        }

        if (messageSpan.IsEmpty ||
            messageSpan.Trim().IsEmpty)
        {
            throw new FormatException(
                "Log message cannot be empty.");
        }

        return new LogEntry(
            index,
            from,
            to,
            levelSpan.ToString(),
            messageSpan.ToString());
    }
    private static ReadOnlySpan<char> ReadNextField(
        ref ReadOnlySpan<char> remaining)
    {
        int separatorIndex = remaining.IndexOf('|');

        if (separatorIndex < 0)
        {
            throw new FormatException(
                "Log line must contain five fields.");
        }

        ReadOnlySpan<char> field =
            remaining[..separatorIndex];

        remaining =
            remaining[(separatorIndex + 1)..];

        return field;
    }
    private static bool IsValidLevel(string level)
    {
        return level is
            "[INFO]" or
            "[WARNING]" or
            "[ERROR]";
    }
    private static bool IsValidLevel(
        ReadOnlySpan<char> level)
    {
        return
            level.SequenceEqual("[INFO]") ||
            level.SequenceEqual("[WARNING]") ||
            level.SequenceEqual("[ERROR]");
    }
}