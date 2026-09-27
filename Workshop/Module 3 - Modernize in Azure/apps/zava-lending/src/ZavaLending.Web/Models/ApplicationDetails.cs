namespace ZavaLending.Web.Models;

public sealed class ApplicationDetails
{
    public long ApplicationId { get; init; }

    public required string ApplicantName { get; init; }

    public required string Email { get; init; }

    public required string Region { get; init; }

    public decimal AnnualIncome { get; init; }

    public required string EmploymentStatus { get; init; }

    public decimal? EmploymentYears { get; init; }

    public int CreditScore { get; init; }

    public decimal? DebtToIncomeRatio { get; init; }

    public required string LoanType { get; init; }

    public decimal RequestedAmount { get; init; }

    public int TermMonths { get; init; }

    public string? LoanPurpose { get; init; }

    public required string Channel { get; init; }

    public DateTime ApplicationDate { get; init; }

    public required string Status { get; init; }
}