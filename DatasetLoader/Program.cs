using Domain;
using Generator;
using Infrastructure;

const string ConnectionStringVariable =
    "OPTIMIZATION_SQL_CONNECTION_STRING";

const int DefaultBatchCapacity = 5_000;

if (args.Length < 3 ||
    args.Length > 4)
{
    PrintUsage();

    return;
}

string normalizedProfile =
    args[0]
        .Replace("-", "")
        .Replace("_", "");

if (!Enum.TryParse(
        normalizedProfile,
        ignoreCase: true,
        out GenerationProfile profile))
{
    Console.Error.WriteLine(
        "Profile must be Standard or ErrorHeavy.");

    return;
}

if (!int.TryParse(
        args[1],
        out int targetCount) ||
    targetCount <= 0)
{
    Console.Error.WriteLine(
        "Target count must be a positive integer.");

    return;
}

if (!int.TryParse(
        args[2],
        out int seedValue))
{
    Console.Error.WriteLine(
        "Seed must be a valid integer.");

    return;
}

int batchCapacity =
    DefaultBatchCapacity;

if (args.Length == 4 &&
    (!int.TryParse(args[3], out batchCapacity) ||
     batchCapacity <= 0))
{
    Console.Error.WriteLine(
        "Batch capacity must be a positive integer.");

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

string datasetName =
    CreateDatasetName(
        profile,
        targetCount,
        seedValue);

var definition =
    new DatasetDefinition(
        Name: datasetName,
        GenerationProfile: profile.ToString(),
        Seed: seedValue,
        TargetCount: targetCount,
        Description:
            $"{profile} controlled dataset with " +
            $"{targetCount:N0} log entries.");

try
{
    var database =
        new SqlDatabase(connectionString);

    DatabaseConnectionInfo connectionInfo =
        await database.VerifyConnectionAsync();

    Console.WriteLine(
        "SQL Server connection succeeded.");

    Console.WriteLine(
        $"Database : {connectionInfo.DatabaseName}");

    Console.WriteLine(
        $"Version  : {connectionInfo.ProductVersion}");

    var datasetRepository =
        new DatasetRepository(database);

    DatasetRegistration registration =
        await datasetRepository.GetOrCreateAsync(
            definition);

    Console.WriteLine();
    Console.WriteLine("===== DATASET =====");
    Console.WriteLine(
        $"DatasetId : {registration.DatasetId}");

    Console.WriteLine(
        $"Name      : {definition.Name}");

    Console.WriteLine(
        $"Profile   : {definition.GenerationProfile}");

    Console.WriteLine(
        $"Seed      : {definition.Seed}");

    Console.WriteLine(
        $"Target    : {definition.TargetCount:N0}");

    Console.WriteLine(
        $"Created   : {registration.Created}");

    var logRepository =
        new LogEntryRepository(database);

    LogEntryStatistics existingStatistics =
        await logRepository.GetStatisticsAsync(
            registration.DatasetId);

    if (existingStatistics.Count == targetCount)
    {
        ValidateStatistics(
            existingStatistics,
            targetCount);

        Console.WriteLine();
        Console.WriteLine(
            "Dataset is already complete.");

        PrintStatistics(
            existingStatistics);

        return;
    }

    if (existingStatistics.Count != 0)
    {
        throw new InvalidOperationException(
            $"Dataset contains {existingStatistics.Count:N0} " +
            $"of the expected {targetCount:N0} rows. " +
            "Partial datasets must be inspected before retrying.");
    }

    Seed seed =
        new(seedValue);

    ILogFactory factory =
        new LogFactory(profile);

    var engine =
        new GeneratorEngine(
            seed,
            targetCount,
            factory);

    using LogEntryBulkWriter writer =
        await LogEntryBulkWriter.CreateAsync(
            database,
            registration.DatasetId);

    var buffer =
        new LogEntryBatchBuffer(
            batchCapacity,
            writer.WriteBatch);

    long generatedChecksum = 0;

    int producedCount =
        engine.Run(log =>
        {
            generatedChecksum = unchecked(
                generatedChecksum +
                log.Index +
                (int)log.From +
                (int)log.To +
                log.Level.Length +
                log.Message.Length);

            buffer.Add(log);
        });

    buffer.Complete();

    if (producedCount != targetCount ||
        buffer.TotalAccepted != targetCount ||
        writer.TotalWritten != targetCount)
    {
        throw new InvalidOperationException(
            "Generator, buffer and writer counts do not match.");
    }

    writer.Complete();

    LogEntryStatistics storedStatistics =
        await logRepository.GetStatisticsAsync(
            registration.DatasetId);

    ValidateStatistics(
        storedStatistics,
        targetCount);

    if (storedStatistics.Checksum != generatedChecksum)
    {
        throw new InvalidOperationException(
            "Generated and stored checksums do not match.");
    }

    Console.WriteLine();
    Console.WriteLine(
        "Dataset generated successfully.");

    Console.WriteLine(
        $"Flushed batches : {buffer.FlushedBatchCount:N0}");

    Console.WriteLine(
        $"Generated checksum: {generatedChecksum}");

    PrintStatistics(
        storedStatistics);
}
catch (Exception exception)
{
    Console.Error.WriteLine();
    Console.Error.WriteLine(
        "Dataset loading failed.");

    Console.Error.WriteLine(
        exception.Message);

    Environment.ExitCode = 1;
}

static string CreateDatasetName(
    GenerationProfile profile,
    int targetCount,
    int seed)
{
    string profileName =
        profile switch
        {
            GenerationProfile.Standard =>
                "standard",

            GenerationProfile.ErrorHeavy =>
                "error-heavy",

            _ => throw new ArgumentOutOfRangeException(
                nameof(profile))
        };

    string sizeName =
        targetCount switch
        {
            5_000 => "5k",
            100_000 => "100k",
            1_000_000 => "1m",
            _ => targetCount.ToString()
        };

    return $"{profileName}-{sizeName}-{seed}";
}

static void ValidateStatistics(
    LogEntryStatistics statistics,
    int targetCount)
{
    if (statistics.Count != targetCount)
    {
        throw new InvalidOperationException(
            "Stored row count does not match the target count.");
    }

    if (statistics.MinimumIndex != 1 ||
        statistics.MaximumIndex != targetCount)
    {
        throw new InvalidOperationException(
            "Stored LogIndex range is invalid.");
    }

    if (statistics.MaximumMessageLength > 4_000)
    {
        throw new InvalidOperationException(
            "A stored message exceeds the SQL column limit.");
    }
}

static void PrintStatistics(
    LogEntryStatistics statistics)
{
    Console.WriteLine(
        $"Stored rows       : {statistics.Count:N0}");

    Console.WriteLine(
        $"Minimum index     : {statistics.MinimumIndex:N0}");

    Console.WriteLine(
        $"Maximum index     : {statistics.MaximumIndex:N0}");

    Console.WriteLine(
        $"Stored checksum   : {statistics.Checksum}");

    Console.WriteLine(
        $"Maximum message   : " +
        $"{statistics.MaximumMessageLength:N0} chars");
}

static void PrintUsage()
{
    Console.WriteLine(
        "Usage:");

    Console.WriteLine(
        "  DatasetLoader " +
        "<profile> <targetCount> <seed> [batchCapacity]");

    Console.WriteLine();

    Console.WriteLine(
        "Example:");

    Console.WriteLine(
        "  DatasetLoader Standard 5000 12345 5000");
}