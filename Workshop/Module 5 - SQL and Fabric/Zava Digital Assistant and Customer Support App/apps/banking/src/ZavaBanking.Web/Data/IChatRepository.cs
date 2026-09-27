using ZavaBanking.Web.Models;

namespace ZavaBanking.Web.Data;

public sealed record PendingTurn(Guid AttemptId, IReadOnlyList<TranscriptMessage> History, TurnResult? Existing);

public interface IChatRepository
{
    Task<PendingTurn> BeginTurnAsync(TurnRequest request, byte[] ownerHash, CancellationToken cancellationToken);
    Task<IReadOnlyList<FaqEntry>> GetFaqAsync(CancellationToken cancellationToken);
    Task<TurnResult> CompleteTurnAsync(TurnRequest request, Guid attemptId, ModelReply reply, CancellationToken cancellationToken);
    Task FailTurnAsync(TurnRequest request, Guid attemptId, string errorCode, CancellationToken cancellationToken);
    Task<EscalationResult> EscalateAsync(EscalationRequest request, byte[] ownerHash, CancellationToken cancellationToken);
}