using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;
using ZavaLending.Web.Models;

namespace ZavaLending.Web.Data;

public sealed class SqlConnectionFactory(
    IConfiguration configuration,
    IOptionsMonitor<DatabaseSettings> databaseSettings) : ISqlConnectionFactory
{
    public SqlConnection CreateConnection()
    {
        var connectionStringOverride = configuration.GetConnectionString("ZavaLending");
        if (!string.IsNullOrWhiteSpace(connectionStringOverride))
        {
            return CreateSecureConnection(connectionStringOverride);
        }

        var settings = databaseSettings.CurrentValue;
        if (string.IsNullOrWhiteSpace(settings.ConnectionString))
        {
            throw new InvalidOperationException(
                "Configure Database:ConnectionString in appsettings.Local.json.");
        }

        var builder = CreateSecureBuilder(settings.ConnectionString);

        switch (settings.Authentication)
        {
            case DatabaseAuthentication.ActiveDirectoryDefault:
                builder.Remove("User ID");
                builder.Remove("Password");
                builder.Authentication = SqlAuthenticationMethod.ActiveDirectoryDefault;
                break;

            case DatabaseAuthentication.SqlPassword:
                if (string.IsNullOrWhiteSpace(settings.UserId) ||
                    string.IsNullOrWhiteSpace(settings.Password))
                {
                    throw new InvalidOperationException(
                        "Database:UserId and Database:Password are required for SQL password authentication.");
                }

                builder.Authentication = SqlAuthenticationMethod.SqlPassword;
                builder.UserID = settings.UserId.Trim();
                builder.Password = settings.Password;
                break;

            default:
                throw new InvalidOperationException(
                    $"Unsupported database authentication mode '{settings.Authentication}'.");
        }

        return new SqlConnection(builder.ConnectionString);
    }

    private static SqlConnection CreateSecureConnection(string connectionString) =>
        new(CreateSecureBuilder(connectionString).ConnectionString);

    private static SqlConnectionStringBuilder CreateSecureBuilder(string connectionString) =>
        new(connectionString)
        {
            Encrypt = true,
            TrustServerCertificate = false
        };
}
