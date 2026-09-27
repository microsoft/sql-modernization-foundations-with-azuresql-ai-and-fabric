using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using ZavaLending.Web.Data;
using ZavaLending.Web.Models;

namespace ZavaLending.Web.Controllers;

public class HomeController(
    ILoanApplicationRepository repository,
    ILoanApplicationScoringRepository scoringRepository,
    IDatabaseCapabilityService capabilityService,
    ILogger<HomeController> logger) : Controller
{
    public async Task<IActionResult> Index(
        [FromQuery] ApplicationSearchFilter filter,
        [FromQuery] long? applicationId,
        CancellationToken cancellationToken)
    {
        if (applicationId is > 0)
        {
            capabilityService.Invalidate();
        }

        return await RenderIndexAsync(
            filter,
            applicationId,
            assessment: null,
            decisionSupportState: null,
            decisionSupportError: null,
            humanDecision: null,
            humanDecisionError: null,
            cancellationToken);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EvaluateApplication(
        [FromForm] ApplicationSearchFilter filter,
        [FromForm] long? applicationId,
        [FromForm] HumanUnderwritingDecision? humanDecision,
        CancellationToken cancellationToken)
    {
        LoanApplicationScoringResult? assessment = null;
        var decisionSupportState = UnderwritingDecisionSupportState.Error;
        string? decisionSupportError;

        if (applicationId is null or <= 0)
        {
            decisionSupportError = "Select a valid application before requesting an assessment.";
        }
        else
        {
            try
            {
                assessment = await scoringRepository.EvaluateAsync(
                    applicationId.Value,
                    cancellationToken);
                decisionSupportState = UnderwritingDecisionSupportState.Completed;
                decisionSupportError = null;
            }
            catch (SqlException exception) when (exception.Number == 2812)
            {
                logger.LogInformation(
                    "The loan application scoring procedure is not deployed. " +
                    "Application {ApplicationId} will remain in Human Review.",
                    applicationId.Value);
                decisionSupportState = UnderwritingDecisionSupportState.SetupRequired;
                decisionSupportError = null;
            }
            catch (SqlException exception) when (!cancellationToken.IsCancellationRequested)
            {
                logger.LogError(
                    exception,
                    "Unable to evaluate loan application {ApplicationId}.",
                    applicationId.Value);
                decisionSupportError =
                    "The underwriting assessment could not be completed. Check the workshop database setup and try again.";
            }
            catch (InvalidOperationException exception)
            {
                logger.LogWarning(
                    exception,
                    "The underwriting assessment could not be completed.");
                decisionSupportError =
                    "The underwriting assessment could not be completed. Check the workshop database setup and try again.";
            }
            catch (ArgumentException exception)
            {
                logger.LogWarning(
                    exception,
                    "The underwriting assessment configuration is invalid.");
                decisionSupportError =
                    "The underwriting assessment could not be completed. Check the workshop database setup and try again.";
            }
            finally
            {
                capabilityService.Invalidate();
            }
        }

        return await RenderIndexAsync(
            filter,
            applicationId,
            assessment,
            decisionSupportState,
            decisionSupportError,
            humanDecision,
            humanDecisionError: null,
            cancellationToken);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RecordHumanDecision(
        [FromForm] HumanDecisionRequest request,
        CancellationToken cancellationToken)
    {
        var humanDecision = request.Decision;
        string? humanDecisionError = null;

        if (request.ApplicationId is null or <= 0)
        {
            humanDecision = null;
            humanDecisionError =
                "Select a valid application before recording the human decision.";
        }
        else if (humanDecision is null ||
                 !Enum.IsDefined(humanDecision.Value))
        {
            humanDecision = null;
            humanDecisionError = "Choose Approve or Decline.";
        }

        var assessment = request.UnderwritingAssessment;
        if (assessment is not null &&
            !IsValidAssessmentForApplication(
                assessment,
                request.ApplicationId))
        {
            assessment = null;
            humanDecisionError =
                "The AI recommendation could not be preserved. Evaluate with AI again.";
        }

        if (request.ApplicationId is > 0 && humanDecision.HasValue)
        {
            try
            {
                var recorded = await repository.RecordHumanDecisionAsync(
                    request.ApplicationId.Value,
                    humanDecision.Value,
                    cancellationToken);

                if (!recorded)
                {
                    humanDecision = null;
                    humanDecisionError =
                        "The selected application was not found. The human decision was not recorded.";
                }
            }
            catch (SqlException exception) when (!cancellationToken.IsCancellationRequested)
            {
                logger.LogError(
                    exception,
                    "Unable to record the human decision for loan application {ApplicationId}.",
                    request.ApplicationId.Value);
                humanDecision = null;
                humanDecisionError =
                    "The human decision could not be recorded. Check the database connection and try again.";
            }
            catch (InvalidOperationException exception)
            {
                logger.LogWarning(
                    exception,
                    "The human decision could not be recorded.");
                humanDecision = null;
                humanDecisionError =
                    "The human decision could not be recorded. Check the database configuration and try again.";
            }
            catch (ArgumentException exception)
            {
                logger.LogWarning(
                    exception,
                    "The human decision is invalid.");
                humanDecision = null;
                humanDecisionError =
                    "The human decision could not be recorded. Choose Approve or Decline.";
            }
        }

        return await RenderIndexAsync(
            request.Filter,
            request.ApplicationId,
            assessment,
            assessment is null
                ? null
                : UnderwritingDecisionSupportState.Completed,
            decisionSupportError: null,
            humanDecision,
            humanDecisionError,
            cancellationToken);
    }

    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }

    private static ApplicationPageViewModel CreateErrorModel(
        ApplicationSearchFilter filter,
        ApplicationPageState state) =>
        new()
        {
            Filter = filter,
            State = state
        };

    private async Task<IActionResult> RenderIndexAsync(
        ApplicationSearchFilter filter,
        long? applicationId,
        LoanApplicationScoringResult? assessment,
        UnderwritingDecisionSupportState? decisionSupportState,
        string? decisionSupportError,
        HumanUnderwritingDecision? humanDecision,
        string? humanDecisionError,
        CancellationToken cancellationToken)
    {
        try
        {
            var search = await repository.SearchAsync(filter, cancellationToken);
            var details = applicationId.HasValue
                ? await repository.GetDetailsAsync(applicationId.Value, cancellationToken)
                : null;
            var capabilities = await capabilityService.GetCapabilitiesAsync(cancellationToken);

            var state = search.TotalApplicationCount == 0
                ? ApplicationPageState.NoApplications
                : search.Results.Count == 0
                    ? ApplicationPageState.NoSearchResults
                    : ApplicationPageState.Ready;
            var effectiveDecisionSupportState = decisionSupportState ??
                (capabilities.ScoringMode == DatabaseScoringMode.Unavailable
                    ? UnderwritingDecisionSupportState.SetupRequired
                    : UnderwritingDecisionSupportState.Ready);

            return View("Index", new ApplicationPageViewModel
            {
                Filter = filter,
                Results = search.Results,
                SelectedApplication = details,
                SelectedApplicationId = applicationId,
                State = state,
                ScoringMode = capabilities.ScoringMode,
                DecisionSupportState = effectiveDecisionSupportState,
                UnderwritingAssessment = assessment,
                DecisionSupportError = decisionSupportError,
                HumanDecision = humanDecision,
                HumanDecisionError = humanDecisionError
            });
        }
        catch (InvalidOperationException exception)
        {
            logger.LogWarning(exception, "The Zava Lending connection is not configured.");
            return View("Index", CreateErrorModel(filter, ApplicationPageState.ConfigurationMissing));
        }
        catch (ArgumentException exception)
        {
            logger.LogWarning(exception, "The Zava Lending connection configuration is invalid.");
            return View("Index", CreateErrorModel(filter, ApplicationPageState.ConfigurationMissing));
        }
        catch (SqlException exception) when (exception.Number == 208)
        {
            logger.LogInformation("The Zava Lending operational tables are not deployed.");
            return View("Index", CreateErrorModel(filter, ApplicationPageState.SchemaUnavailable));
        }
        catch (SqlException exception) when (!cancellationToken.IsCancellationRequested)
        {
            logger.LogError(exception, "Unable to query the Zava Lending database.");
            return View("Index", CreateErrorModel(filter, ApplicationPageState.DatabaseUnavailable));
        }
    }

    private static bool IsValidAssessmentForApplication(
        LoanApplicationScoringResult assessment,
        long? applicationId) =>
        assessment.ApplicationId == applicationId &&
        assessment.Decision is
            "Approved" or
            "ConditionallyApproved" or
            "ManualReview" or
            "Denied" &&
        assessment.RiskScore is >= 0 and <= 100 &&
        assessment.RiskCategory is "Low" or "Medium" or "High" or "Critical" &&
        !string.IsNullOrWhiteSpace(assessment.AssessmentNarrative);
}
