using ZavaLending.Web.Models;

namespace ZavaLending.Web.Data;

public interface ILoanApplicationScoringRepository
{
    Task<LoanApplicationScoringResult> EvaluateAsync(
        long applicationId,
        CancellationToken cancellationToken);
}
