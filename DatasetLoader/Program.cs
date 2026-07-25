using Infrastructure;

const string ConnectionStringVariable =
    "OPTIMIZATION_SQL_CONNECTION_STRING";

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

    DatabaseConnectionInfo result =
        await database.VerifyConnectionAsync();

    Console.WriteLine(
        "SQL Server connection succeeded.");

    Console.WriteLine(
        $"Database : {result.DatabaseName}");

    Console.WriteLine(
        $"Version  : {result.ProductVersion}");

    Console.WriteLine(
        $"Edition  : {result.Edition}");
}
catch (Exception exception)
{
    Console.Error.WriteLine(
        "SQL Server connection failed.");

    Console.Error.WriteLine(
        exception.Message);

    Environment.ExitCode = 1;
}