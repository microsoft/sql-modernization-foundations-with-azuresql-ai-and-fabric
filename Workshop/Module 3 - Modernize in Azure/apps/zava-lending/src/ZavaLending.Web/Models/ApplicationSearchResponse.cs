namespace ZavaLending.Web.Models;

public sealed record ApplicationSearchResponse(
    long TotalApplicationCount,
    IReadOnlyList<ApplicationSearchResult> Results);