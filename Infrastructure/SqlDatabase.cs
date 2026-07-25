using Microsoft.Data.SqlClient;

namespace Infrastructure;

public sealed class SqlDatabase
{
    private readonly string _connectionString;

    public SqlDatabase(string connectionString)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(
            connectionString);

        _connectionString = connectionString;
    }

    public async Task<DatabaseConnectionInfo>
        VerifyConnectionAsync(
            CancellationToken cancellationToken = default)
    {
        await using var connection =
            new SqlConnection(_connectionString);

        await connection.OpenAsync(cancellationToken);

        await using SqlCommand command =
            connection.CreateCommand();

        command.CommandText =
            """
            SELECT
                DB_NAME(),
                CAST(
                    SERVERPROPERTY('ProductVersion')
                    AS NVARCHAR(128)
                ),
                CAST(
                    SERVERPROPERTY('Edition')
                    AS NVARCHAR(128)
                );
            """;

        await using SqlDataReader reader =
            await command.ExecuteReaderAsync(
                cancellationToken);

        if (!await reader.ReadAsync(cancellationToken))
        {
            throw new InvalidOperationException(
                "SQL Server did not return connection information.");
        }

        return new DatabaseConnectionInfo(
            DatabaseName: reader.GetString(0),
            ProductVersion: reader.GetString(1),
            Edition: reader.GetString(2));
    }
    
}
public sealed record DatabaseConnectionInfo(
    string DatabaseName,
    string ProductVersion,
    string Edition);