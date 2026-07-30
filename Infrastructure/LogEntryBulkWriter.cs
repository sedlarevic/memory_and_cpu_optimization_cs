using System.Data;
using Domain;
using Microsoft.Data.SqlClient;

namespace Infrastructure;

public sealed class LogEntryBulkWriter : IDisposable
{
private readonly long _datasetId;

    private readonly SqlConnection _connection;
    private readonly SqlBulkCopy _bulkCopy;

    private bool _completed;
    private bool _disposed;

    public long TotalWritten { get; private set; }

    private LogEntryBulkWriter(
        long datasetId,
        SqlConnection connection)
    {
        _datasetId = datasetId;
        _connection = connection;

        _bulkCopy =
            new SqlBulkCopy(
                connection,
                SqlBulkCopyOptions.TableLock |
                SqlBulkCopyOptions.CheckConstraints |
                SqlBulkCopyOptions.UseInternalTransaction,
                externalTransaction: null);

        _bulkCopy.DestinationTableName =
            "[dbo].[LogEntries]";

        _bulkCopy.BulkCopyTimeout = 60;

        _bulkCopy.ColumnMappings.Add(
            "DatasetId",
            "DatasetId");

        _bulkCopy.ColumnMappings.Add(
            "LogIndex",
            "LogIndex");

        _bulkCopy.ColumnMappings.Add(
            "FromState",
            "FromState");

        _bulkCopy.ColumnMappings.Add(
            "ToState",
            "ToState");

        _bulkCopy.ColumnMappings.Add(
            "Level",
            "Level");

        _bulkCopy.ColumnMappings.Add(
            "Message",
            "Message");
    }

    public static async Task<LogEntryBulkWriter>
        CreateAsync(
            SqlDatabase database,
            long datasetId,
            CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(database);

        if (datasetId <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(datasetId),
                "DatasetId must be greater than zero.");
        }

        SqlConnection connection =
            await database.OpenConnectionAsync(
                cancellationToken);

        try
        {
            return new LogEntryBulkWriter(
                datasetId,
                connection);
        }
        catch
        {
            await connection.DisposeAsync();

            throw;
        }
    }

    public void WriteBatch(
        LogEntry[] entries,
        int count)
    {
        ObjectDisposedException.ThrowIf(
            _disposed,
            this);

        if (_completed)
        {
            throw new InvalidOperationException(
                "Cannot write after the bulk writer is completed.");
        }

        ArgumentNullException.ThrowIfNull(entries);

        if (count <= 0 ||
            count > entries.Length)
        {
            throw new ArgumentOutOfRangeException(
                nameof(count),
                "Count must be between one and the array length.");
        }

        using DataTable table =
            CreateDataTable(count);

        for (int index = 0;
             index < count;
             index++)
        {
            LogEntry entry =
                entries[index] ??
                throw new InvalidOperationException(
                    $"Batch entry at position {index} is null.");

            DataRow row =
                table.NewRow();

            row["DatasetId"] = _datasetId;
            row["LogIndex"] = entry.Index;
            row["FromState"] = (byte)entry.From;
            row["ToState"] = (byte)entry.To;
            row["Level"] = entry.Level;
            row["Message"] = entry.Message;

            table.Rows.Add(row);
        }

        _bulkCopy.BatchSize = count;

        _bulkCopy.WriteToServer(table);

        TotalWritten += count;
    }

    public void Complete()
    {
        ObjectDisposedException.ThrowIf(
            _disposed,
            this);

        _completed = true;
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        ((IDisposable)_bulkCopy).Dispose();
        _connection.Dispose();

        _disposed = true;
    }

    private static DataTable CreateDataTable(
        int capacity)
    {
        var table =
            new DataTable
            {
                MinimumCapacity = capacity
            };

        table.Columns.Add(
            "DatasetId",
            typeof(long));

        table.Columns.Add(
            "LogIndex",
            typeof(int));

        table.Columns.Add(
            "FromState",
            typeof(byte));

        table.Columns.Add(
            "ToState",
            typeof(byte));

        table.Columns.Add(
            "Level",
            typeof(string));

        table.Columns.Add(
            "Message",
            typeof(string));

        return table;
    }
}