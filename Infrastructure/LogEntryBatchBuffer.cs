using Domain;

namespace Infrastructure;

public sealed class LogEntryBatchBuffer
{
    private readonly LogEntry[] _entries;

    private readonly Action<LogEntry[], int>
        _flushBatch;

    private int _count;
    private bool _completed;

    public int Capacity => _entries.Length;

    public int PendingCount => _count;

    public long TotalAccepted { get; private set; }

    public int FlushedBatchCount { get; private set; }

    public LogEntryBatchBuffer(
        int capacity,
        Action<LogEntry[], int> flushBatch)
    {
        if (capacity <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(capacity),
                "Batch capacity must be greater than zero.");
        }

        ArgumentNullException.ThrowIfNull(flushBatch);

        _entries = new LogEntry[capacity];
        _flushBatch = flushBatch;
    }

    public void Add(LogEntry entry)
    {
        ArgumentNullException.ThrowIfNull(entry);

        if (_completed)
        {
            throw new InvalidOperationException(
                "Cannot add log entries after the buffer is completed.");
        }

        _entries[_count] = entry;
        _count++;
        TotalAccepted++;

        if (_count == _entries.Length)
        {
            Flush();
        }
    }

    public void Complete()
    {
        if (_completed)
        {
            return;
        }

        Flush();

        _completed = true;
    }

    private void Flush()
    {
        if (_count == 0)
        {
            return;
        }

        int flushedCount = _count;

        _flushBatch(
            _entries,
            flushedCount);

        Array.Clear(
            _entries,
            0,
            flushedCount);

        _count = 0;
        FlushedBatchCount++;
    }
}