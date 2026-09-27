using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using ZavaBanking.Web.Models;
using ZavaBanking.Web.Services;

namespace ZavaBanking.Web.Controllers;

[ApiController]
[Route("api/chat")]
[ValidateAntiForgeryToken]
[EnableRateLimiting("chat")]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class ChatController(ChatService service, ILogger<ChatController> logger) : ControllerBase
{
    [HttpPost("messages")]
    [RequestSizeLimit(16384)]
    public Task<IActionResult> Send(TurnRequest request) => Execute(async token =>
        await service.SendAsync(request, Request.Headers["X-Chat-Key"].FirstOrDefault(), token));

    [HttpPost("escalations")]
    [RequestSizeLimit(4096)]
    public Task<IActionResult> Escalate(EscalationRequest request) => Execute(async token =>
        await service.EscalateAsync(request, Request.Headers["X-Chat-Key"].FirstOrDefault(), token));

    private async Task<IActionResult> Execute(Func<CancellationToken, Task<object>> action)
    {
        using var operation = new CancellationTokenSource(TimeSpan.FromSeconds(120));
        try { return Ok(await action(operation.Token)); }
        catch (ChatProblem error) { return StatusCode(error.Status, new { code = error.Code, message = error.Message }); }
        catch (OperationCanceledException)
        {
            return StatusCode(504, new { code = "timeout", message = "The request timed out. Retry the same request; it will not create duplicate messages or follow-up requests." });
        }
        catch (Exception error)
        {
            logger.LogWarning("Chat dependency failure ({FailureType}); correlation {TraceId}.", error.GetType().Name, HttpContext.TraceIdentifier);
            return StatusCode(503, new { code = "dependency", message = "We could not confirm this request was saved. Check the workshop SQL and AI setup, then retry. After an interruption, a pending reply can take up to 3 minutes to become retryable." });
        }
    }
}