namespace Domain;

public static class LogLineFormat
{
    public static string Format(LogEntry log)
    {
        ArgumentNullException.ThrowIfNull(log);

        return
            $"{log.Index}|" +
            $"{log.Level}|" +
            $"{log.From}|" +
            $"{log.To}|" +
            $"{log.Message}";
    }
}