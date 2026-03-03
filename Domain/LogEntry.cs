namespace Domain;

public class LogEntry
{
    public int Index { get; set; }
    public State From { get; set; }
    public State To { get; set; }
    public string Level { get; set; }
    public string Message { get; set; }

    public LogEntry(int index, State from, State to, string level, string message)
    {
        Index =  index;
        From = from;
        To = to;
        Level = level;
        Message = message;
    }
}