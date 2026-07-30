using System.Data;
using Microsoft.Data.SqlClient;

namespace Infrastructure;

public sealed class DatasetRepository
{
      private readonly SqlDatabase _database;

    public DatasetRepository(
        SqlDatabase database)
    {
        ArgumentNullException.ThrowIfNull(
            database);

        _database = database;
    }
    public async Task<long> GetIdByNameAsync(
        string name,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        await using SqlConnection connection =
            await _database.OpenConnectionAsync(
                cancellationToken);

        await using SqlCommand command =
            connection.CreateCommand();

        command.CommandText =
            """
            SELECT DatasetId
            FROM dbo.Datasets
            WHERE Name = @Name;
            """;

        command.Parameters.Add(
            "@Name",
            SqlDbType.NVarChar,
            150).Value = name;

        object? result =
            await command.ExecuteScalarAsync(
                cancellationToken);

        if (result is null ||
            result is DBNull)
        {
            throw new InvalidOperationException(
                $"Dataset '{name}' does not exist.");
        }

        return Convert.ToInt64(result);
    }
    public async Task<DatasetRegistration>
        GetOrCreateAsync(
            DatasetDefinition dataset,
            CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(dataset);

        Validate(dataset);

        await using SqlConnection connection =
            await _database.OpenConnectionAsync(
                cancellationToken);

        await using SqlCommand command =
            connection.CreateCommand();

        command.CommandText =
            """
            SELECT
                DatasetId,
                GenerationProfile,
                Seed,
                TargetCount
            FROM dbo.Datasets
            WHERE Name = @Name;
            """;

        command.Parameters.Add(
            "@Name",
            SqlDbType.NVarChar,
            150).Value = dataset.Name;

        await using SqlDataReader reader =
            await command.ExecuteReaderAsync(
                cancellationToken);

        if (await reader.ReadAsync(cancellationToken))
        {
            long datasetId =
                reader.GetInt64(0);

            string storedProfile =
                reader.GetString(1);

            int storedSeed =
                reader.GetInt32(2);

            int storedTargetCount =
                reader.GetInt32(3);

            if (storedProfile != dataset.GenerationProfile ||
                storedSeed != dataset.Seed ||
                storedTargetCount != dataset.TargetCount)
            {
                throw new InvalidOperationException(
                    $"Dataset '{dataset.Name}' already exists " +
                    "with different generation parameters.");
            }

            return new DatasetRegistration(
                datasetId,
                Created: false);
        }

        await reader.DisposeAsync();
        await connection.DisposeAsync();

        long createdDatasetId =
            await CreateAsync(
                dataset,
                cancellationToken);

        return new DatasetRegistration(
            createdDatasetId,
            Created: true);
    }
    
    public async Task<long> CreateAsync(
        DatasetDefinition dataset,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(
            dataset);

        Validate(dataset);

        await using SqlConnection connection =
            await _database.OpenConnectionAsync(
                cancellationToken);

        await using SqlCommand command =
            connection.CreateCommand();

        command.CommandText =
            """
            INSERT INTO dbo.Datasets
            (
                Name,
                GenerationProfile,
                Seed,
                TargetCount,
                Description
            )
            OUTPUT INSERTED.DatasetId
            VALUES
            (
                @Name,
                @GenerationProfile,
                @Seed,
                @TargetCount,
                @Description
            );
            """;

        command.Parameters
            .Add(
                "@Name",
                SqlDbType.NVarChar,
                150)
            .Value = dataset.Name;

        command.Parameters
            .Add(
                "@GenerationProfile",
                SqlDbType.VarChar,
                30)
            .Value = dataset.GenerationProfile;

        command.Parameters
            .Add(
                "@Seed",
                SqlDbType.Int)
            .Value = dataset.Seed;

        command.Parameters
            .Add(
                "@TargetCount",
                SqlDbType.Int)
            .Value = dataset.TargetCount;

        command.Parameters
            .Add(
                "@Description",
                SqlDbType.NVarChar,
                500)
            .Value =
                dataset.Description is null
                    ? DBNull.Value
                    : dataset.Description;

        object? result =
            await command.ExecuteScalarAsync(
                cancellationToken);

        if (result is null ||
            result is DBNull)
        {
            throw new InvalidOperationException(
                "SQL Server did not return DatasetId.");
        }

        return Convert.ToInt64(result);
    }

    private static void Validate(
        DatasetDefinition dataset)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(
            dataset.Name);

        ArgumentException.ThrowIfNullOrWhiteSpace(
            dataset.GenerationProfile);

        if (dataset.Name.Length > 150)
        {
            throw new ArgumentException(
                "Dataset name cannot exceed 150 characters.",
                nameof(dataset));
        }

        if (dataset.GenerationProfile is not
            ("Standard" or "ErrorHeavy"))
        {
            throw new ArgumentException(
                "Generation profile must be " +
                "'Standard' or 'ErrorHeavy'.",
                nameof(dataset));
        }

        if (dataset.TargetCount <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(dataset),
                "Target count must be greater than zero.");
        }

        if (dataset.Description?.Length > 500)
        {
            throw new ArgumentException(
                "Dataset description cannot exceed 500 characters.",
                nameof(dataset));
        }
    }
}