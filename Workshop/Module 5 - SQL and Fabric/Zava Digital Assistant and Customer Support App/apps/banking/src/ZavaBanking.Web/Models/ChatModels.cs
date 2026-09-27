namespace ZavaBanking.Web.Models;

public sealed record FaqEntry(string Id, string Topic, string Question, string Answer, string Source);
public sealed record SourceReference(string Id, string Title, string Url);
public sealed record TranscriptMessage(int Sequence, Guid RequestId, string Role, string Content,
    DateTime CreatedAt, string Status, IReadOnlyList<SourceReference> Sources);
public sealed record ConversationSnapshot(int Version, Guid SessionId, IReadOnlyList<TranscriptMessage> Messages);
public sealed record ModelReply(string Text, IReadOnlyList<SourceReference> Sources, string Model,
    int InputTokens, int OutputTokens, string FinishReason);
public sealed record TurnRequest(Guid SessionId, Guid RequestId, string Message);
public sealed record EscalationRequest(Guid SessionId, Guid RequestId, string Email);
public sealed record TurnResult(Guid SessionId, Guid RequestId, string Reply, IReadOnlyList<SourceReference> Sources);
public sealed record EscalationResult(Guid EscalationId, Guid SessionId, DateTime CreatedAt);
public sealed class ChatProblem(int status, string code, string message) : Exception(message)
{
    public int Status { get; } = status;
    public string Code { get; } = code;
}