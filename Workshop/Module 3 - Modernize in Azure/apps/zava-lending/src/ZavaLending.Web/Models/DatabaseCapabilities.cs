namespace ZavaLending.Web.Models;

public sealed record DatabaseCapabilities(
    string? Platform,
    string? ServerName,
    string? DatabaseName,
    string? SqlGeneration,
    string? HistoricalLoanSearchMode,
    DatabaseScoringMode ScoringMode,
    bool DetectionSucceeded)
{
    public static DatabaseCapabilities Fallback { get; } =
        new(
            null,
            null,
            null,
            null,
            null,
            DatabaseScoringMode.Unavailable,
            false);

    public string? ApplicationsScoringMode =>
        ScoringMode switch
        {
            DatabaseScoringMode.AiAssisted => "AI-Assisted Scoring",
            _ => "Human Review & Scoring"
        };

    public string? DecisionSupportMode =>
        ScoringMode switch
        {
            DatabaseScoringMode.AiAssisted => "AI-Assisted",
            _ => "Human Review"
        };
}

public enum DatabaseScoringMode
{
    Unavailable,
    AiAssisted
}

public enum DatabaseCapabilityWorkspace
{
    Applications,
    HistoricalLoans
}
