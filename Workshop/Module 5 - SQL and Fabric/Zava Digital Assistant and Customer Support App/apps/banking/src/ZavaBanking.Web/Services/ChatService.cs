using System.ClientModel;
using System.Diagnostics;
using ZavaBanking.Web.Data;
using ZavaBanking.Web.Models;

namespace ZavaBanking.Web.Services;

public sealed class ChatService(IChatRepository repository, IChatModel model, ILogger<ChatService> logger)
{
    public async Task<TurnResult> SendAsync(TurnRequest request, string? ownerKey, CancellationToken cancellationToken)
    {
        ChatPolicy.Validate(request);
        var ownerHash = ChatPolicy.OwnerHash(ownerKey);
        var elapsed = Stopwatch.StartNew();
        var stage = "saving visitor message to SQL";
        logger.LogInformation("Chat request {RequestId}: {Stage}.", request.RequestId, stage);
        PendingTurn pending;
        try { pending = await repository.BeginTurnAsync(request, ownerHash, cancellationToken); }
        catch (Exception error)
        {
            logger.LogWarning("Chat request {RequestId} failed during {Stage} after {ElapsedMs} ms ({FailureType}).",
                request.RequestId, stage, elapsed.ElapsedMilliseconds, error.GetType().Name);
            throw;
        }
        if (pending.Existing is not null)
        {
            logger.LogInformation("Chat request {RequestId}: returning previously saved reply.", request.RequestId);
            return pending.Existing;
        }
        try
        {
            stage = "reading FAQ from SQL";
            logger.LogInformation("Chat request {RequestId}: {Stage}.", request.RequestId, stage);
            var faq = await repository.GetFaqAsync(cancellationToken);
            stage = "waiting for Azure OpenAI";
            logger.LogInformation("Chat request {RequestId}: {Stage}.", request.RequestId, stage);
            var reply = await model.ReplyAsync(faq, pending.History, cancellationToken);
            stage = "saving assistant reply to SQL";
            logger.LogInformation("Chat request {RequestId}: {Stage}.", request.RequestId, stage);
            var result = await repository.CompleteTurnAsync(request, pending.AttemptId, reply, cancellationToken);
            logger.LogInformation("Chat request {RequestId}: reply saved after {ElapsedMs} ms.", request.RequestId, elapsed.ElapsedMilliseconds);
            return result;
        }
        catch (Exception error)
        {
            logger.LogWarning("Chat request {RequestId} failed during {Stage} after {ElapsedMs} ms ({FailureType}; HTTP status {Status}).",
                request.RequestId, stage, elapsed.ElapsedMilliseconds, error.GetType().Name,
                error is ClientResultException clientError ? clientError.Status : (int?)null);
            var code = error is ChatProblem problem ? problem.Code : error is OperationCanceledException ? "timeout" : "dependency_failure";
            try
            {
                using var recovery = new CancellationTokenSource(TimeSpan.FromSeconds(10));
                await repository.FailTurnAsync(request, pending.AttemptId, code, recovery.Token);
            }
            catch
            {
                logger.LogWarning("Failure state could not be saved for request {RequestId}. Its SQL lease allows retry after expiry.", request.RequestId);
            }
            throw;
        }
    }

    public Task<EscalationResult> EscalateAsync(EscalationRequest request, string? ownerKey, CancellationToken cancellationToken) =>
        repository.EscalateAsync(request with { Email = ChatPolicy.ValidateEmail(request.Email) }, ChatPolicy.OwnerHash(ownerKey), cancellationToken);
}