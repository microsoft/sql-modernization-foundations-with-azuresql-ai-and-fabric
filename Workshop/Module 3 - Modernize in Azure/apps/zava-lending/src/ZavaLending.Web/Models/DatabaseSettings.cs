namespace ZavaLending.Web.Models;

public sealed class DatabaseSettings
{
    public const string SectionName = "Database";

    public string? ConnectionString { get; init; }

    public DatabaseAuthentication Authentication { get; init; } =
        DatabaseAuthentication.ActiveDirectoryDefault;

    public string? UserId { get; init; }

    public string? Password { get; init; }
}

public enum DatabaseAuthentication
{
    ActiveDirectoryDefault,
    SqlPassword
}
