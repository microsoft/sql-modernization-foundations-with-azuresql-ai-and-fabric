namespace ZavaLending.Web.Models;

public sealed class LoanApplicationScoringResult
{
    public long ApplicationId { get; init; }

    public required string Decision { get; init; }

    public decimal RiskScore { get; init; }

    public required string RiskCategory { get; init; }

    public decimal? RecommendedAmount { get; init; }

    public decimal? RecommendedRate { get; init; }

    public required string AssessmentNarrative { get; init; }

    public int? SimilarLoansAnalyzed { get; init; }

    public decimal? SimilarLoanApprovalRate { get; init; }

    public decimal? SimilarLoanAvgDefaultRate { get; init; }

    public int ProcessingTimeMs { get; init; }

    public bool HasSimilarLoanEvidence =>
        SimilarLoansAnalyzed.HasValue ||
        SimilarLoanApprovalRate.HasValue ||
        SimilarLoanAvgDefaultRate.HasValue;
}
