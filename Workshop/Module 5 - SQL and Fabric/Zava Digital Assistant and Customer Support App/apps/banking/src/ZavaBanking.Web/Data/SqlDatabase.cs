using Dapper;
using Microsoft.Data.SqlClient;

namespace ZavaBanking.Web.Data;

public sealed class SqlDatabase(IConfiguration configuration, ILogger<SqlDatabase>? logger = null)
{
    public async Task<SqlConnection> OpenAsync(CancellationToken cancellationToken)
    {
        var configured = configuration.GetConnectionString("ZavaBanking");
        if (string.IsNullOrWhiteSpace(configured))
            throw new InvalidOperationException("Set ConnectionStrings__ZavaBanking in apps/banking/.env and run dotnet run --project ./src/ZavaBanking.Web -- --initialize from the apps/banking folder.");
        var settings = new SqlConnectionStringBuilder(configured)
        {
            Encrypt = SqlConnectionEncryptOption.Mandatory,
            TrustServerCertificate = false,
            ConnectTimeout = 30
        };
        if (settings.Authentication != SqlAuthenticationMethod.ActiveDirectoryDefault ||
            !string.IsNullOrEmpty(settings.Password) || settings.IntegratedSecurity)
            throw new InvalidOperationException("Fabric SQL requires Authentication=Active Directory Default.");
        var connection = new SqlConnection(settings.ConnectionString);
        try
        {
            logger?.LogInformation("SQL connection: opening with Microsoft Entra authentication from the local developer credential chain.");
            await connection.OpenAsync(cancellationToken);
            logger?.LogInformation("SQL connection: authenticated and open.");
            return connection;
        }
        catch (Exception error)
        {
            logger?.LogWarning("SQL connection: opening failed ({FailureType}; canceled token {Canceled}; SQL error {SqlNumber}).",
                error.GetType().Name, cancellationToken.IsCancellationRequested,
                error is SqlException sqlError ? sqlError.Number : (int?)null);
            await connection.DisposeAsync();
            throw;
        }
    }

    public async Task InitializeAsync(CancellationToken cancellationToken)
    {
        var assembly = typeof(SqlDatabase).Assembly;
        var resource = assembly.GetManifestResourceNames().Single(name => name.EndsWith("initialize.sql"));
        using var reader = new StreamReader(assembly.GetManifestResourceStream(resource)!);
        var sql = await reader.ReadToEndAsync(cancellationToken);
        Console.WriteLine("SQL initialization: opening connection with the identity from az login.");
        await using var connection = await OpenAsync(cancellationToken);
        Console.WriteLine("SQL initialization: connected. Executing the schema and FAQ seed transaction.");
        await connection.ExecuteAsync(new CommandDefinition(sql, commandTimeout: 120, cancellationToken: cancellationToken));
        Console.WriteLine("SQL initialization: schema and FAQ seed transaction completed.");
    }

    public async Task<int> CheckAsync(CancellationToken cancellationToken)
    {
        Console.WriteLine("SQL verification: opening connection.");
        await using var connection = await OpenAsync(cancellationToken);
        Console.WriteLine("SQL verification: connected. Checking tables and FAQ count.");
        return await connection.QuerySingleAsync<int>(new CommandDefinition("""
            SELECT COUNT(*) FROM dbo.FaqEntries;
            SELECT TOP (0) * FROM dbo.ChatSessions;
            SELECT TOP (0) * FROM dbo.ChatMessages;
            SELECT TOP (0) * FROM dbo.ChatEscalations;
            """, cancellationToken: cancellationToken));
    }
}