namespace ZavaLending.Web.Models;

public sealed record HistoricalLoanSearchResponse(
    long TotalHistoricalLoanCount,
    IReadOnlyList<HistoricalLoanSearchResult> Results);
