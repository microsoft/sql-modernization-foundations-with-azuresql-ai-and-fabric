namespace ZavaSupport.Web.Models;

public sealed record TranscriptMessage(int Sequence, string Role, string Content, DateTime CreatedAt, string Status);

public sealed record Escalation(
    Guid EscalationId,
    Guid SessionId,
    string Email,
    DateTime CreatedAt,
    DateTime ReceivedAt,
    int MessageCount,
    IReadOnlyList<TranscriptMessage> Messages);

public sealed record FeedStatus(string State, string Message);

public sealed record FeedEvent(string Name, object Payload);
