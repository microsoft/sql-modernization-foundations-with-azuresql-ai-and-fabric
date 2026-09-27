using System.Net.Mail;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using ZavaBanking.Web.Models;

namespace ZavaBanking.Web.Services;

public static class ChatPolicy
{
    public const int MaxUserCharacters = 1200;
    public const int MaxReplyCharacters = 2400;
    public const int MaxTurns = 12;
    public const int MaxSnapshotBytes = 30 * 1024;
    public const int ReplyReserveBytes = MaxReplyCharacters * 6 + 6144;
    public static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    public static byte[] OwnerHash(string? key)
    {
        if (key is null || key.Length != 64 || !key.All(char.IsAsciiHexDigit))
            throw new ChatProblem(401, "session_key", "This chat is not accessible. Start a new chat.");
        return SHA256.HashData(Convert.FromHexString(key));
    }

    public static void Validate(TurnRequest request)
    {
        if (request.SessionId == Guid.Empty || request.RequestId == Guid.Empty ||
            string.IsNullOrWhiteSpace(request.Message) || request.Message.Length > MaxUserCharacters)
            throw new ChatProblem(400, "message", $"Enter a message of 1 to {MaxUserCharacters} characters.");
    }

    public static string ValidateEmail(string? email)
    {
        var trimmed = email?.Trim() ?? "";
        if (trimmed.Length > 254 || trimmed.Contains('\r') || trimmed.Contains('\n') ||
            !MailAddress.TryCreate(trimmed, out var address) || address.Address != trimmed ||
            !address.Host.Contains('.') || trimmed.Contains(' '))
            throw new ChatProblem(400, "email", "Enter a valid email address.");
        return trimmed;
    }

    public static string SerializeSnapshot(Guid sessionId, IReadOnlyList<TranscriptMessage> messages, int reserve = 0)
    {
        var json = JsonSerializer.Serialize(new ConversationSnapshot(1, sessionId, messages), Json);
        if (Encoding.UTF8.GetByteCount(json) + reserve > MaxSnapshotBytes)
            throw new ChatProblem(409, "conversation_limit", "This chat has reached its size limit. Request follow-up or start a new chat.");
        return json;
    }
}