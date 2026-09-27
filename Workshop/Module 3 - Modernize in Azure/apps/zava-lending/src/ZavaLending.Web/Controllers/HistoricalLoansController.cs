using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using ZavaLending.Web.Data;
using ZavaLending.Web.Models;

namespace ZavaLending.Web.Controllers;

public sealed class HistoricalLoansController(
    IHistoricalLoanRepository repository,
    IDatabaseCapabilityService capabilityService,
    ILogger<HistoricalLoansController> logger) : Controller
{
    public async Task<IActionResult> Index(
        [FromQuery] HistoricalLoanSearchFilter filter,
        [FromQuery] long? loanId,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return View(CreateErrorModel(filter, HistoricalLoanPageState.InvalidSearch));
        }

        try
        {
            var search = await repository.SearchAsync(filter, cancellationToken);
            var selectedLoan = loanId.HasValue
                ? search.Results.SingleOrDefault(loan => loan.LoanId == loanId.Value)
                : null;

            var state = search.TotalHistoricalLoanCount == 0
                ? HistoricalLoanPageState.NoHistoricalLoans
                : !filter.HasPrompt
                    ? HistoricalLoanPageState.SearchRequired
                    : search.Results.Count == 0
                        ? HistoricalLoanPageState.NoSearchResults
                        : HistoricalLoanPageState.Ready;

            return View(new HistoricalLoanPageViewModel
            {
                Filter = filter,
                Results = search.Results,
                SelectedLoan = selectedLoan,
                SelectedLoanId = loanId,
                State = state
            });
        }
        catch (InvalidOperationException exception)
        {
            logger.LogWarning(exception, "The Zava Lending connection is not configured.");
            return View(CreateErrorModel(filter, HistoricalLoanPageState.ConfigurationMissing));
        }
        catch (ArgumentException exception)
        {
            logger.LogWarning(exception, "The Zava Lending connection configuration is invalid.");
            return View(CreateErrorModel(filter, HistoricalLoanPageState.ConfigurationMissing));
        }
        catch (SqlException exception) when (exception.Number == 2812)
        {
            logger.LogInformation("The historical loan search procedure is not deployed.");
            return View(CreateErrorModel(filter, HistoricalLoanPageState.SearchUnavailable));
        }
        catch (SqlException exception) when (exception.Number is 207 or 208)
        {
            logger.LogInformation("The historical loan schema is not deployed.");
            return View(CreateErrorModel(filter, HistoricalLoanPageState.SchemaUnavailable));
        }
        catch (SqlException exception) when (!cancellationToken.IsCancellationRequested)
        {
            logger.LogError(exception, "Unable to query historical loans.");
            return View(CreateErrorModel(filter, HistoricalLoanPageState.DatabaseUnavailable));
        }
        finally
        {
            if (filter.HasPrompt)
            {
                capabilityService.Invalidate();
            }
        }
    }

    private static HistoricalLoanPageViewModel CreateErrorModel(
        HistoricalLoanSearchFilter filter,
        HistoricalLoanPageState state) =>
        new()
        {
            Filter = filter,
            State = state
        };
}
