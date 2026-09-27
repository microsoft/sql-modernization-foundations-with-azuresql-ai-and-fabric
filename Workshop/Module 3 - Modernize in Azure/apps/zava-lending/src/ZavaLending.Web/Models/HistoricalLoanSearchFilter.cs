using System.ComponentModel.DataAnnotations;

namespace ZavaLending.Web.Models;

public sealed class HistoricalLoanSearchFilter : IValidatableObject
{
    [StringLength(1000)]
    public string? Prompt { get; set; }

    public string? LoanType { get; set; }

    public string? LoanOutcome { get; set; }

    [Range(300, 850)]
    public int? MinCreditScore { get; set; }

    [Range(300, 850)]
    public int? MaxCreditScore { get; set; }

    [Range(typeof(decimal), "0", "9999999999999999.99")]
    public decimal? MinAmount { get; set; }

    [Range(typeof(decimal), "0", "9999999999999999.99")]
    public decimal? MaxAmount { get; set; }

    public DateTime? DateFrom { get; set; }

    public DateTime? DateTo { get; set; }

    [Range(1, 50)]
    public int TopN { get; set; } = 10;

    public bool HasPrompt => !string.IsNullOrWhiteSpace(Prompt);

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (MinCreditScore > MaxCreditScore)
        {
            yield return new ValidationResult(
                "Minimum credit score cannot exceed maximum credit score.",
                [nameof(MinCreditScore), nameof(MaxCreditScore)]);
        }

        if (MinAmount > MaxAmount)
        {
            yield return new ValidationResult(
                "Minimum amount cannot exceed maximum amount.",
                [nameof(MinAmount), nameof(MaxAmount)]);
        }

        if (DateFrom > DateTo)
        {
            yield return new ValidationResult(
                "Start date cannot be after end date.",
                [nameof(DateFrom), nameof(DateTo)]);
        }
    }
}
