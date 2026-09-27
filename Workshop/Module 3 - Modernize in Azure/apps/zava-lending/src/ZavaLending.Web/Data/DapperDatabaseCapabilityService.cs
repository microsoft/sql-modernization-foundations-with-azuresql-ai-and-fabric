using Dapper;
using Microsoft.Data.SqlClient;
using ZavaLending.Web.Models;

namespace ZavaLending.Web.Data;

public sealed class DapperDatabaseCapabilityService(
    ISqlConnectionFactory connectionFactory,
    ILogger<DapperDatabaseCapabilityService> logger) : IDatabaseCapabilityService
{
    private static readonly TimeSpan SuccessCacheDuration = TimeSpan.FromSeconds(30);
    private static readonly TimeSpan FailureCacheDuration = TimeSpan.FromSeconds(10);

    private const string PlatformMetadataSql = """
        SELECT
            CONVERT(INT, SERVERPROPERTY('EngineEdition')) AS EngineEdition,
            CONVERT(NVARCHAR(128), SERVERPROPERTY('Edition')) AS Edition,
            CONVERT(NVARCHAR(128), SERVERPROPERTY('ProductVersion')) AS ProductVersion,
            CONVERT(NVARCHAR(128), SERVERPROPERTY('ServerName')) AS ServerName,
            CONVERT(NVARCHAR(128), DB_NAME()) AS DatabaseName,
            CONVERT(INT, d.compatibility_level) AS CompatibilityLevel
        FROM sys.databases AS d
        WHERE d.name = DB_NAME();
        """;

    private const string AzureServiceTierSql = """
        SELECT TOP (1)
            edition AS ServiceTier
        FROM sys.database_service_objectives
        WHERE database_id = DB_ID();
        """;

    private const string LoanSearchMetadataSql = """
        SELECT
            OBJECT_ID(N'dbo.usp_LoanSearch', N'P') AS ObjectId,
            OBJECT_DEFINITION(OBJECT_ID(N'dbo.usp_LoanSearch', N'P')) AS Definition;
        """;

    private const string LoanScoringMetadataSql = """
        SELECT
            OBJECT_ID(N'dbo.usp_ScoreLoanApplication', N'P') AS ObjectId,
            OBJECT_DEFINITION(OBJECT_ID(N'dbo.usp_ScoreLoanApplication', N'P')) AS Definition;
        """;

    private readonly SemaphoreSlim refreshLock = new(1, 1);
    private CacheEntry? cache;
    private long cacheGeneration;

    public void Invalidate()
    {
        Interlocked.Increment(ref cacheGeneration);
        Volatile.Write(ref cache, null);
    }

    public async Task<DatabaseCapabilities> GetCapabilitiesAsync(
        CancellationToken cancellationToken = default)
    {
        var cached = Volatile.Read(ref cache);
        if (cached is not null && cached.ExpiresAt > DateTimeOffset.UtcNow)
        {
            return cached.Capabilities;
        }

        await refreshLock.WaitAsync(cancellationToken);
        try
        {
            cached = Volatile.Read(ref cache);
            if (cached is not null && cached.ExpiresAt > DateTimeOffset.UtcNow)
            {
                return cached.Capabilities;
            }

            var refreshGeneration = Volatile.Read(ref cacheGeneration);
            var detection = await DetectAsync(cancellationToken);
            var cacheDuration = detection.HadFailure
                ? FailureCacheDuration
                : SuccessCacheDuration;

            if (refreshGeneration == Volatile.Read(ref cacheGeneration))
            {
                Volatile.Write(
                    ref cache,
                    new CacheEntry(
                        detection.Capabilities,
                        DateTimeOffset.UtcNow.Add(cacheDuration)));
            }

            return detection.Capabilities;
        }
        finally
        {
            refreshLock.Release();
        }
    }

    private async Task<DetectionResult> DetectAsync(CancellationToken cancellationToken)
    {
        SqlConnection connection;
        try
        {
            connection = connectionFactory.CreateConnection();
        }
        catch (Exception exception) when (IsExpectedDetectionFailure(exception))
        {
            LogFailure("database configuration", exception);
            return new DetectionResult(DatabaseCapabilities.Fallback, true);
        }

        await using var connectionScope = connection;
        try
        {
            await connection.OpenAsync(cancellationToken);
        }
        catch (Exception exception) when (IsExpectedDetectionFailure(exception))
        {
            LogFailure("database connection", exception);
            return new DetectionResult(DatabaseCapabilities.Fallback, true);
        }

        var hadFailure = false;
        PlatformMetadata? platformMetadata = null;
        string? serviceTier = null;
        ProcedureMetadata? searchProcedure = null;
        ProcedureMetadata? scoringProcedure = null;

        try
        {
            platformMetadata = await connection.QuerySingleOrDefaultAsync<PlatformMetadata>(
                new CommandDefinition(
                    PlatformMetadataSql,
                    cancellationToken: cancellationToken));
        }
        catch (Exception exception) when (IsExpectedDetectionFailure(exception))
        {
            hadFailure = true;
            LogFailure("platform metadata", exception);
        }

        if (platformMetadata?.EngineEdition == 5)
        {
            try
            {
                serviceTier = await connection.QuerySingleOrDefaultAsync<string>(
                    new CommandDefinition(
                        AzureServiceTierSql,
                        cancellationToken: cancellationToken));
            }
            catch (Exception exception) when (IsExpectedDetectionFailure(exception))
            {
                hadFailure = true;
                LogFailure("Azure SQL service tier", exception);
            }
        }

        try
        {
            searchProcedure = await connection.QuerySingleOrDefaultAsync<ProcedureMetadata>(
                new CommandDefinition(
                    LoanSearchMetadataSql,
                    cancellationToken: cancellationToken));
        }
        catch (Exception exception) when (IsExpectedDetectionFailure(exception))
        {
            hadFailure = true;
            LogFailure("loan search metadata", exception);
        }

        try
        {
            scoringProcedure = await connection.QuerySingleOrDefaultAsync<ProcedureMetadata>(
                new CommandDefinition(
                    LoanScoringMetadataSql,
                    cancellationToken: cancellationToken));
        }
        catch (Exception exception) when (IsExpectedDetectionFailure(exception))
        {
            hadFailure = true;
            LogFailure("loan scoring metadata", exception);
        }

        var isAzureSqlDatabase = platformMetadata?.EngineEdition == 5;
        var isHyperscale = isAzureSqlDatabase &&
            string.Equals(serviceTier, "Hyperscale", StringComparison.OrdinalIgnoreCase);

        var capabilities = new DatabaseCapabilities(
            GetPlatform(platformMetadata?.EngineEdition, isHyperscale),
            GetConnectionLabel(platformMetadata?.ServerName, connection.DataSource),
            GetConnectionLabel(platformMetadata?.DatabaseName, connection.Database),
            GetSqlGeneration(
                platformMetadata?.CompatibilityLevel,
                isAzureSqlDatabase),
            GetSearchMode(searchProcedure),
            GetScoringMode(scoringProcedure),
            !hadFailure);

        return new DetectionResult(capabilities, hadFailure);
    }

    private static string? GetPlatform(int? engineEdition, bool isHyperscale) =>
        engineEdition switch
        {
            8 => "Azure SQL Managed Instance",
            5 when isHyperscale => "Azure SQL Database Hyperscale",
            5 => "Azure SQL Database",
            2 or 3 or 4 => "SQL Server",
            _ => null
        };

    private static string? GetSqlGeneration(
        int? compatibilityLevel,
        bool isAzureSqlDatabase)
    {
        if (isAzureSqlDatabase)
        {
            return "Evergreen SQL";
        }

        return compatibilityLevel switch
        {
            160 => "SQL 2022",
            170 => "SQL 2025",
            int value => $"Compatibility {value}",
            null => null
        };
    }

    private static string? GetSearchMode(ProcedureMetadata? procedure)
    {
        if (procedure?.ObjectId is null || procedure.Definition is null)
        {
            return null;
        }

        if (ContainsAny(
                procedure.Definition,
                "VECTOR_SEARCH",
                "AI_GENERATE_EMBEDDINGS"))
        {
            return "Hybrid Semantic Search";
        }

        if (ContainsAny(
                procedure.Definition,
                "FREETEXTTABLE",
                "CONTAINSTABLE"))
        {
            return "Full-Text Search";
        }

        return "Loan Search";
    }

    private static DatabaseScoringMode GetScoringMode(ProcedureMetadata? procedure)
    {
        if (procedure?.ObjectId is null || procedure.Definition is null)
        {
            return DatabaseScoringMode.Unavailable;
        }

        return procedure.Definition.Contains(
            "sp_invoke_external_rest_endpoint",
            StringComparison.OrdinalIgnoreCase)
            ? DatabaseScoringMode.AiAssisted
            : DatabaseScoringMode.Unavailable;
    }

    private static bool ContainsAny(string value, params string[] markers) =>
        markers.Any(marker =>
            value.Contains(marker, StringComparison.OrdinalIgnoreCase));

    private static string? GetConnectionLabel(string? detectedValue, string fallbackValue)
    {
        var value = string.IsNullOrWhiteSpace(detectedValue)
            ? fallbackValue
            : detectedValue;

        return string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim();
    }

    private static bool IsExpectedDetectionFailure(Exception exception) =>
        exception is SqlException or InvalidOperationException or ArgumentException or TimeoutException;

    private void LogFailure(string capability, Exception exception) =>
        logger.LogWarning(
            "Database capability detection could not read {Capability} ({ExceptionType}).",
            capability,
            exception.GetType().Name);

    private sealed record PlatformMetadata(
        int EngineEdition,
        string? Edition,
        string? ProductVersion,
        string? ServerName,
        string? DatabaseName,
        int CompatibilityLevel);

    private sealed record ProcedureMetadata(int? ObjectId, string? Definition);

    private sealed record DetectionResult(
        DatabaseCapabilities Capabilities,
        bool HadFailure);

    private sealed record CacheEntry(
        DatabaseCapabilities Capabilities,
        DateTimeOffset ExpiresAt);
}
