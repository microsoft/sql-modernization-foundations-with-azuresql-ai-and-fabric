namespace ZavaLending.Web.Models;

public enum HistoricalLoanPageState
{
    Ready,
    SearchRequired,
    ConfigurationMissing,
    SchemaUnavailable,
    SearchUnavailable,
    InvalidSearch,
    NoHistoricalLoans,
    NoSearchResults,
    DatabaseUnavailable
}

public sealed class HistoricalLoanPageViewModel
{
    public required HistoricalLoanSearchFilter Filter { get; init; }

    public IReadOnlyList<HistoricalLoanSearchResult> Results { get; init; } = [];

    public HistoricalLoanSearchResult? SelectedLoan { get; init; }

    public long? SelectedLoanId { get; init; }

    public HistoricalLoanPageState State { get; init; } = HistoricalLoanPageState.SearchRequired;
}
