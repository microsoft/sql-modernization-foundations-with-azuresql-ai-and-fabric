using ZavaLending.Web.Models;

namespace ZavaLending.Web.Data;

public interface IHistoricalLoanRepository
{
    Task<HistoricalLoanSearchResponse> SearchAsync(
        HistoricalLoanSearchFilter filter,
        CancellationToken cancellationToken);
}
