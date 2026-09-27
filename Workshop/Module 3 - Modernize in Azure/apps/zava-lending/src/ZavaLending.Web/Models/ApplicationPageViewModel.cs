namespace ZavaLending.Web.Models;

public enum ApplicationPageState
{
    Ready,
    ConfigurationMissing,
    SchemaUnavailable,
    NoApplications,
    NoSearchResults,
    DatabaseUnavailable
}

public enum UnderwritingDecisionSupportState
{
    Ready,
    Completed,
    SetupRequired,
    Error
}

public enum HumanUnderwritingDecision
{
    Approved,
    Declined
}

public sealed class HumanDecisionRequest
{
    public ApplicationSearchFilter Filter { get; init; } = new();

    public long? ApplicationId { get; init; }

    public HumanUnderwritingDecision? Decision { get; init; }

    public LoanApplicationScoringResult? UnderwritingAssessment { get; init; }
}

public sealed class ApplicationPageViewModel
{
    public required ApplicationSearchFilter Filter { get; init; }

    public IReadOnlyList<ApplicationSearchResult> Results { get; init; } = [];

    public ApplicationDetails? SelectedApplication { get; init; }

    public long? SelectedApplicationId { get; init; }

    public ApplicationPageState State { get; init; } = ApplicationPageState.Ready;

    public DatabaseScoringMode ScoringMode { get; init; } = DatabaseScoringMode.Unavailable;

    public UnderwritingDecisionSupportState DecisionSupportState { get; init; } =
        UnderwritingDecisionSupportState.Ready;

    public LoanApplicationScoringResult? UnderwritingAssessment { get; init; }

    public string? DecisionSupportError { get; init; }

    public HumanUnderwritingDecision? HumanDecision { get; init; }

    public string? HumanDecisionError { get; init; }
}