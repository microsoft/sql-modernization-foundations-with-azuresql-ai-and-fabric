using Dapper;
using ZavaLending.Web.Models;

namespace ZavaLending.Web.Data;

public sealed class DapperLoanApplicationRepository(ISqlConnectionFactory connectionFactory)
    : ILoanApplicationRepository
{
    private const int ResultLimit = 50;

    private const string SearchSql = """
        SELECT COUNT_BIG(*)
        FROM dbo.LoanApplications;

        SELECT TOP (@ResultLimit)
            la.ApplicationId,
            CONCAT(a.FirstName, N' ', a.LastName) AS ApplicantName,
            la.LoanType,
            la.RequestedAmount,
            la.Status,
            a.CreditScore,
            a.Region
        FROM dbo.LoanApplications AS la
        INNER JOIN dbo.Applicants AS a
            ON a.ApplicantId = la.ApplicantId
        WHERE (@CustomerPattern IS NULL
                OR a.FirstName LIKE @CustomerPattern
                OR a.LastName LIKE @CustomerPattern
                OR CONCAT(a.FirstName, N' ', a.LastName) LIKE @CustomerPattern
                OR a.Email LIKE @CustomerPattern)
          AND (@Status IS NULL OR la.Status = @Status)
          AND (@LoanType IS NULL OR la.LoanType = @LoanType)
        ORDER BY la.ApplicationDate DESC, la.ApplicationId DESC;
        """;

    private const string DetailsSql = """
        SELECT
            la.ApplicationId,
            CONCAT(a.FirstName, N' ', a.LastName) AS ApplicantName,
            a.Email,
            a.Region,
            a.AnnualIncome,
            a.EmploymentStatus,
            a.EmploymentYears,
            a.CreditScore,
            a.DebtToIncomeRatio,
            la.LoanType,
            la.RequestedAmount,
            la.TermMonths,
            la.LoanPurpose,
            la.Channel,
            la.ApplicationDate,
            la.Status
        FROM dbo.LoanApplications AS la
        INNER JOIN dbo.Applicants AS a
            ON a.ApplicantId = la.ApplicantId
        WHERE la.ApplicationId = @ApplicationId;
        """;

    private const string RecordHumanDecisionSql = """
        UPDATE dbo.LoanApplications
        SET Status = @Status
        WHERE ApplicationId = @ApplicationId;
        """;

    public async Task<ApplicationSearchResponse> SearchAsync(
        ApplicationSearchFilter filter,
        CancellationToken cancellationToken)
    {
        await using var connection = connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);

        var customer = Normalize(filter.Customer);
        var parameters = new
        {
            ResultLimit,
            CustomerPattern = customer is null ? null : $"%{customer}%",
            Status = Normalize(filter.Status),
            LoanType = Normalize(filter.LoanType)
        };

        var command = new CommandDefinition(
            SearchSql,
            parameters,
            cancellationToken: cancellationToken);

        using var results = await connection.QueryMultipleAsync(command);
        var totalCount = await results.ReadSingleAsync<long>();
        var applications = (await results.ReadAsync<ApplicationSearchResult>()).AsList();

        return new ApplicationSearchResponse(totalCount, applications);
    }

    public async Task<ApplicationDetails?> GetDetailsAsync(
        long applicationId,
        CancellationToken cancellationToken)
    {
        await using var connection = connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);

        var command = new CommandDefinition(
            DetailsSql,
            new { ApplicationId = applicationId },
            cancellationToken: cancellationToken);

        return await connection.QuerySingleOrDefaultAsync<ApplicationDetails>(command);
    }

    public async Task<bool> RecordHumanDecisionAsync(
        long applicationId,
        HumanUnderwritingDecision decision,
        CancellationToken cancellationToken)
    {
        var status = decision switch
        {
            HumanUnderwritingDecision.Approved => "Approved",
            HumanUnderwritingDecision.Declined => "Declined",
            _ => throw new ArgumentOutOfRangeException(
                nameof(decision),
                decision,
                "The human underwriting decision is not supported.")
        };

        await using var connection = connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);

        var command = new CommandDefinition(
            RecordHumanDecisionSql,
            new
            {
                ApplicationId = applicationId,
                Status = status
            },
            cancellationToken: cancellationToken);

        return await connection.ExecuteAsync(command) == 1;
    }

    private static string? Normalize(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
