namespace ZavaLending.Web.Models;

public sealed class ApplicationSearchFilter
{
    public string? Customer { get; set; }

    public string? Status { get; set; }

    public string? LoanType { get; set; }

    public bool HasFilters =>
        !string.IsNullOrWhiteSpace(Customer) ||
        !string.IsNullOrWhiteSpace(Status) ||
        !string.IsNullOrWhiteSpace(LoanType);
}