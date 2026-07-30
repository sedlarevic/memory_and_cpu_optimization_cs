using Domain;
using Infrastructure;
using Resolver;

const string ConnectionStringVariable =
    "OPTIMIZATION_SQL_CONNECTION_STRING";

if (args.Length is < 1 or > 2 ||
    !long.TryParse(args[0], out long datasetId) ||
    datasetId <= 0)
{
    PrintUsage();
    return;
}

string resolverMode =
    args.Length == 2
        ? args[1].ToLowerInvariant()
        : "span";

if (resolverMode is not "span" and not "split")
{
    Console.Error.WriteLine(
        "Resolver must be 'span' or 'split'.");

    return;
}

string? connectionString =
    Environment.GetEnvironmentVariable(
        ConnectionStringVariable);

if (string.IsNullOrWhiteSpace(connectionString))
{
    Console.Error.WriteLine(
        $"Environment variable " +
        $"'{ConnectionStringVariable}' is not configured.");

    return;
}

try
{
    var database =
        new SqlDatabase(connectionString);

    var repository =
        new LogEntryRepository(database);

    LogEntryStatistics storedStatistics =
        await repository.GetStatisticsAsync(
            datasetId);

    int expectedNextIndex = 1;
    long resolvedCount = 0;
    long resolvedChecksum = 0;

    Console.WriteLine(
        "===== DATABASE RESOLVER TEST =====");

    Console.WriteLine(
        $"DatasetId : {datasetId}");

    Console.WriteLine(
        $"Resolver  : {resolverMode}");

    Console.WriteLine(
        $"Expected  : {storedStatistics.Count:N0} rows");

    LogEntryReadResult readResult =
        await repository.ReadAsync(
            datasetId,
            storedLog =>
            {
                if (storedLog.Index != expectedNextIndex)
                {
                    throw new InvalidOperationException(
                        $"Unexpected LogIndex " +
                        $"{storedLog.Index}. " +
                        $"Expected {expectedNextIndex}.");
                }

                string line =
                    LogLineFormat.Format(storedLog);

                LogEntry resolvedLog =
                    resolverMode == "span"
                        ? LogResolver.ResolveWithSpan(line)
                        : LogResolver.ResolveWithSplit(line);

                resolvedChecksum = unchecked(
                    resolvedChecksum +
                    resolvedLog.Index +
                    (int)resolvedLog.From +
                    (int)resolvedLog.To +
                    resolvedLog.Level.Length +
                    resolvedLog.Message.Length);

                resolvedCount++;
                expectedNextIndex++;
            });

    if (readResult.Count !=
        storedStatistics.Count)
    {
        throw new InvalidOperationException(
            "Read row count does not match " +
            "the stored row count.");
    }

    if (readResult.Checksum !=
        storedStatistics.Checksum)
    {
        throw new InvalidOperationException(
            "Reader checksum does not match " +
            "the stored checksum.");
    }

    if (resolvedCount !=
        storedStatistics.Count)
    {
        throw new InvalidOperationException(
            "Resolved row count does not match " +
            "the stored row count.");
    }

    if (resolvedChecksum !=
        storedStatistics.Checksum)
    {
        throw new InvalidOperationException(
            "Resolver checksum does not match " +
            "the stored checksum.");
    }

    Console.WriteLine();
    Console.WriteLine(
        "Dataset resolved successfully.");

    Console.WriteLine(
        $"Read rows         : {readResult.Count:N0}");

    Console.WriteLine(
        $"Resolved rows     : {resolvedCount:N0}");

    Console.WriteLine(
        $"Stored checksum   : {storedStatistics.Checksum}");

    Console.WriteLine(
        $"Reader checksum   : {readResult.Checksum}");

    Console.WriteLine(
        $"Resolver checksum : {resolvedChecksum}");

    Console.WriteLine(
        "Validation        : PASSED");
}
catch (Exception exception)
{
    Console.Error.WriteLine();
    Console.Error.WriteLine(
        "Dataset resolving failed.");

    Console.Error.WriteLine(
        exception.Message);

    Environment.ExitCode = 1;
}

static void PrintUsage()
{
    Console.Error.WriteLine(
        "Usage: DatasetReader <dataset-id> [span|split]");
}