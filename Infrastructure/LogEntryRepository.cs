using System.Data;
using Domain;
using Microsoft.Data.SqlClient;

namespace Infrastructure;

public sealed class LogEntryRepository
{
    private readonly SqlDatabase _database;

    public LogEntryRepository(
        SqlDatabase database)
    {
        ArgumentNullException.ThrowIfNull(database);

        _database = database;
    }

    public async Task<LogEntryStatistics>
        GetStatisticsAsync(
            long datasetId,
            CancellationToken cancellationToken = default)
    {
        if (datasetId <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(datasetId));
        }

        await using SqlConnection connection =
            await _database.OpenConnectionAsync(
                cancellationToken);

        await using SqlCommand command =
            connection.CreateCommand();

        command.CommandText =
            """
            SELECT
                COUNT_BIG(*),
                MIN(LogIndex),
                MAX(LogIndex),
                COALESCE
                (
                    SUM
                    (
                        CAST(LogIndex AS BIGINT)
                        + CAST(FromState AS BIGINT)
                        + CAST(ToState AS BIGINT)
                        + LEN(Level)
                        + LEN(Message)
                    ),
                    0
                ),
                COALESCE(MAX(LEN(Message)), 0)
            FROM dbo.LogEntries
            WHERE DatasetId = @DatasetId;
            """;

        command.Parameters.Add(
            "@DatasetId",
            SqlDbType.BigInt).Value = datasetId;

        await using SqlDataReader reader =
            await command.ExecuteReaderAsync(
                cancellationToken);

        if (!await reader.ReadAsync(cancellationToken))
        {
            throw new InvalidOperationException(
                "SQL Server did not return log statistics.");
        }

        return new LogEntryStatistics(
            Count: reader.GetInt64(0),

            MinimumIndex:
                reader.IsDBNull(1)
                    ? null
                    : reader.GetInt32(1),

            MaximumIndex:
                reader.IsDBNull(2)
                    ? null
                    : reader.GetInt32(2),

            Checksum: reader.GetInt64(3),

            MaximumMessageLength:
                reader.GetInt32(4));
    }
    public async Task<LogEntryReadResult> ReadAsync(
        long datasetId,
        Action<LogEntry> consumer,
        CancellationToken cancellationToken = default)
    {
        if (datasetId <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(datasetId));
        }

        ArgumentNullException.ThrowIfNull(consumer);

        await using SqlConnection connection =
            await _database.OpenConnectionAsync(
                cancellationToken);

        await using SqlCommand command =
            connection.CreateCommand();

        command.CommandText =
            """
            SELECT
                LogIndex,
                FromState,
                ToState,
                Level,
                Message
            FROM dbo.LogEntries
            WHERE DatasetId = @DatasetId
            ORDER BY LogIndex;
            """;

        command.Parameters.Add(
            "@DatasetId",
            SqlDbType.BigInt).Value = datasetId;

        await using SqlDataReader reader =
            await command.ExecuteReaderAsync(
                CommandBehavior.SequentialAccess,
                cancellationToken);

        long count = 0;
        long checksum = 0;

        while (await reader.ReadAsync(cancellationToken))
        {
            int logIndex =
                reader.GetInt32(0);

            State from =
                (State)reader.GetByte(1);

            State to =
                (State)reader.GetByte(2);

            string level =
                reader.GetString(3);

            string message =
                reader.GetString(4);

            var log =
                new LogEntry(
                    logIndex,
                    from,
                    to,
                    level,
                    message);

            checksum = unchecked(
                checksum +
                log.Index +
                (int)log.From +
                (int)log.To +
                log.Level.Length +
                log.Message.Length);

            consumer(log);
            count++;
        }

        return new LogEntryReadResult(
            Count: count,
            Checksum: checksum);
    }
}
public sealed record LogEntryStatistics(
    long Count,
    int? MinimumIndex,
    int? MaximumIndex,
    long Checksum,
    int MaximumMessageLength);
    
public sealed record LogEntryReadResult(
    long Count,
    long Checksum);