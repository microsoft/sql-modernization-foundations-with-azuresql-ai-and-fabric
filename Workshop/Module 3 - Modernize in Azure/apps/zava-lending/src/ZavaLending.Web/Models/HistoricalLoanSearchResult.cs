namespace ZavaLending.Web.Models;

public sealed class HistoricalLoanSearchResult
{
    public long LoanId { get; init; }

    public required string LoanType { get; init; }

    public decimal RequestedAmount { get; init; }

    public decimal? ApprovedAmount { get; init; }

    public int CreditScore { get; init; }

    public decimal? DebtToIncomeRatio { get; init; }

    public required string LoanOutcome { get; init; }

    public decimal? DefaultRate { get; init; }

    public string? LoanPurpose { get; init; }

    public DateTime ApplicationDate { get; init; }

    public string? NarrativePreview { get; init; }

    public string? LoanNarrative { get; init; }

    public double? SemanticDistance { get; init; }

    public required string SemanticRelevance { get; init; }
}
