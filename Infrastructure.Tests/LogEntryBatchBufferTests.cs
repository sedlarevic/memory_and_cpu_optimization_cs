using Domain;

namespace Infrastructure.Tests;

public class LogEntryBatchBufferTests
{
    [Fact]
    public void Complete_FlushesFullAndRemainingBatches()
    {
        var flushedBatches =
            new List<int[]>();

        var buffer =
            new LogEntryBatchBuffer(
                capacity: 3,
                flushBatch: (entries, count) =>
                {
                    int[] indexes =
                        new int[count];

                    for (int index = 0;
                         index < count;
                         index++)
                    {
                        indexes[index] =
                            entries[index].Index;
                    }

                    flushedBatches.Add(indexes);
                });

        for (int index = 1;
             index <= 5;
             index++)
        {
            buffer.Add(
                CreateLogEntry(index));
        }

        buffer.Complete();

        Assert.Equal(
            2,
            flushedBatches.Count);

        Assert.Equal(
            [1, 2, 3],
            flushedBatches[0]);

        Assert.Equal(
            [4, 5],
            flushedBatches[1]);

        Assert.Equal(
            5,
            buffer.TotalAccepted);

        Assert.Equal(
            2,
            buffer.FlushedBatchCount);

        Assert.Equal(
            0,
            buffer.PendingCount);
    }

    [Fact]
    public void FullBatch_IsFlushedImmediately()
    {
        int flushCount = 0;
        int flushedEntryCount = 0;

        var buffer =
            new LogEntryBatchBuffer(
                capacity: 2,
                flushBatch: (_, count) =>
                {
                    flushCount++;
                    flushedEntryCount += count;
                });

        buffer.Add(CreateLogEntry(1));

        Assert.Equal(
            0,
            flushCount);

        Assert.Equal(
            1,
            buffer.PendingCount);

        buffer.Add(CreateLogEntry(2));

        Assert.Equal(
            1,
            flushCount);

        Assert.Equal(
            2,
            flushedEntryCount);

        Assert.Equal(
            0,
            buffer.PendingCount);
    }

    [Fact]
    public void Complete_WithEmptyBuffer_DoesNotFlush()
    {
        int flushCount = 0;

        var buffer =
            new LogEntryBatchBuffer(
                capacity: 10,
                flushBatch: (_, _) =>
                {
                    flushCount++;
                });

        buffer.Complete();
        buffer.Complete();

        Assert.Equal(
            0,
            flushCount);

        Assert.Equal(
            0,
            buffer.FlushedBatchCount);
    }

    [Fact]
    public void Add_AfterComplete_Throws()
    {
        var buffer =
            new LogEntryBatchBuffer(
                capacity: 10,
                flushBatch: (_, _) =>
                {
                });

        buffer.Complete();

        Assert.Throws<InvalidOperationException>(
            () => buffer.Add(
                CreateLogEntry(1)));
    }

    [Fact]
    public void Constructor_WithInvalidCapacity_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => new LogEntryBatchBuffer(
                capacity: 0,
                flushBatch: (_, _) =>
                {
                }));
    }

    private static LogEntry CreateLogEntry(
        int index)
    {
        return new LogEntry(
            index,
            State.Idle,
            State.RequestReceived,
            "[INFO]",
            $"[INFO] Idle -> RequestReceived | {index}");
    }
}