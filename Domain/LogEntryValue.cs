namespace Domain;

public readonly struct LogEntryValue
{
    public int Index { get; }

    public State From { get; }

    public State To { get; }

    public string Level { get; }

    public string Message { get; }

    public LogEntryValue(
        int index,
        State from,
        State to,
        string level,
        string message)
    {
        ArgumentNullException.ThrowIfNull(level);
        ArgumentNullException.ThrowIfNull(message);

        Index = index;
        From = from;
        To = to;
        Level = level;
        Message = message;
    }
}