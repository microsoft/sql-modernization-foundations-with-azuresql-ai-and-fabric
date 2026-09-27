using System.Data;
using Dapper;
using ZavaLending.Web.Models;

namespace ZavaLending.Web.Data;

public sealed class DapperHistoricalLoanRepository(ISqlConnectionFactory connectionFactory)
    : IHistoricalLoanRepository
{
    private const string CountSql = """
        SELECT COUNT_BIG(*)
        FROM dbo.LoanHistory;
        """;

    public async Task<HistoricalLoanSearchResponse> SearchAsync(
        HistoricalLoanSearchFilter filter,
        CancellationToken cancellationToken)
    {
        await using var connection = connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);

        var totalCount = await connection.ExecuteScalarAsync<long>(
            new CommandDefinition(CountSql, cancellationToken: cancellationToken));

        var prompt = Normalize(filter.Prompt);
        if (prompt is null)
        {
            return new HistoricalLoanSearchResponse(totalCount, []);
        }

        var parameters = new
        {
            Prompt = prompt,
            LoanType = Normalize(filter.LoanType),
            MinCreditScore = filter.MinCreditScore,
            MaxCreditScore = filter.MaxCreditScore,
            LoanOutcome = Normalize(filter.LoanOutcome),
            MinAmount = filter.MinAmount,
            MaxAmount = filter.MaxAmount,
            DateFrom = filter.DateFrom,
            DateTo = filter.DateTo,
            filter.TopN
        };

        var command = new CommandDefinition(
            "dbo.usp_LoanSearch",
            parameters,
            commandType: CommandType.StoredProcedure,
            cancellationToken: cancellationToken);

        var loans = (await connection.QueryAsync<HistoricalLoanSearchResult>(command)).AsList();
        return new HistoricalLoanSearchResponse(totalCount, loans);
    }

    private static string? Normalize(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
