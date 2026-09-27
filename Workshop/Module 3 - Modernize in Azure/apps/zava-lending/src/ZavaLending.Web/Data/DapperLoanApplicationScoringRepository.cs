using System.Data;
using Dapper;
using ZavaLending.Web.Models;

namespace ZavaLending.Web.Data;

public sealed class DapperLoanApplicationScoringRepository(
    ISqlConnectionFactory connectionFactory) : ILoanApplicationScoringRepository
{
    public async Task<LoanApplicationScoringResult> EvaluateAsync(
        long applicationId,
        CancellationToken cancellationToken)
    {
        await using var connection = connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);

        var command = new CommandDefinition(
            "dbo.usp_ScoreLoanApplication",
            new { ApplicationId = applicationId },
            commandType: CommandType.StoredProcedure,
            cancellationToken: cancellationToken);

        return await connection.QuerySingleAsync<LoanApplicationScoringResult>(command);
    }
}
