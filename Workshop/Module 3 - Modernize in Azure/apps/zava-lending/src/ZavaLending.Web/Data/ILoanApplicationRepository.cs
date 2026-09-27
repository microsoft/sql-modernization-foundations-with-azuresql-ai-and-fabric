using ZavaLending.Web.Models;

namespace ZavaLending.Web.Data;

public interface ILoanApplicationRepository
{
    Task<ApplicationSearchResponse> SearchAsync(
        ApplicationSearchFilter filter,
        CancellationToken cancellationToken);

    Task<ApplicationDetails?> GetDetailsAsync(
        long applicationId,
        CancellationToken cancellationToken);

    Task<bool> RecordHumanDecisionAsync(
        long applicationId,
        HumanUnderwritingDecision decision,
        CancellationToken cancellationToken);
}
