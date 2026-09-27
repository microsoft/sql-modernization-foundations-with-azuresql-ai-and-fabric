namespace ZavaLending.Web.Models;

public sealed class ApplicationSearchResult
{
    public long ApplicationId { get; init; }

    public required string ApplicantName { get; init; }

    public required string LoanType { get; init; }

    public decimal RequestedAmount { get; init; }

    public required string Status { get; init; }

    public int CreditScore { get; init; }

    public required string Region { get; init; }
}