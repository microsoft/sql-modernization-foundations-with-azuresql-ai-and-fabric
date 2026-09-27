using System.Data;
using System.Security.Cryptography;
using System.Text.Json;
using Dapper;
using Microsoft.Data.SqlClient;
using ZavaBanking.Web.Models;
using ZavaBanking.Web.Services;

namespace ZavaBanking.Web.Data;

public sealed class SqlChatRepository(SqlDatabase database) : IChatRepository
{
    public async Task<PendingTurn> BeginTurnAsync(TurnRequest request, byte[] ownerHash, CancellationToken cancellationToken)
    {
        await using var connection = await database.OpenAsync(cancellationToken);
        await using var transaction = (SqlTransaction)await connection.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);
        var session = await LockSession(connection, transaction, request.SessionId, cancellationToken);
        if (session is null)
        {
            await connection.ExecuteAsync(Command("""
                INSERT dbo.ChatSessions (SessionId, OwnerHash) VALUES (@SessionId, @OwnerHash);
                """, new { request.SessionId, OwnerHash = ownerHash }, transaction, cancellationToken));
            session = new SessionRow { SessionId = request.SessionId, OwnerHash = ownerHash };
        }
        VerifyOwner(session, ownerHash);
        var messages = await ReadMessages(connection, transaction, request.SessionId, cancellationToken);
        var existing = messages.SingleOrDefault(message => message.RequestId == request.RequestId && message.Role == "user");
        if (existing is not null && existing.Content != request.Message)
            throw new ChatProblem(409, "request_conflict", "Retry must use the original message.");
        if (existing?.Status == "Completed")
        {
            var answer = messages.Single(message => message.RequestId == request.RequestId && message.Role == "assistant");
            await transaction.CommitAsync(cancellationToken);
            return new PendingTurn(Guid.Empty, [], Result(request, answer));
        }
        if (session.State != "Open")
            throw new ChatProblem(409, "closed", "This conversation is closed. Start a new chat.");

        var now = await connection.QuerySingleAsync<DateTime>(Command("SELECT SYSUTCDATETIME();", null, transaction, cancellationToken));
        var unresolved = messages.LastOrDefault(message => message.Role == "user" && message.Status != "Completed");
        if (unresolved is not null && unresolved.RequestId != request.RequestId)
            throw new ChatProblem(409, "unfinished_turn", "Retry the previous message before sending another, or start a new chat.");
        if (existing?.Status == "Pending" && existing.LeaseUntil > now)
            throw new ChatProblem(409, "pending", "The reply is still being saved. Wait a moment, then retry.");

        var attemptId = Guid.NewGuid();
        var attempts = existing is null ? [] : Deserialize<List<AttemptRecord>>(existing.AttemptsJson);
        CloseAttempt(attempts, now, "Interrupted", "lease_expired");
        attempts.Add(new AttemptRecord(attemptId, now, null, "Pending", null));
        if (existing is null)
        {
            if (messages.Count(message => message.Role == "user") >= ChatPolicy.MaxTurns)
                throw new ChatProblem(409, "conversation_limit", "This chat has reached its turn limit. Request follow-up or start a new chat.");
            existing = new MessageRow
            {
                RequestId = request.RequestId, Sequence = session.NextSequence, Role = "user",
                Content = request.Message, CreatedAt = now, Status = "Pending"
            };
            messages.Add(existing);
            ChatPolicy.SerializeSnapshot(request.SessionId, messages.Select(ToTranscript).ToArray(), ChatPolicy.ReplyReserveBytes);
            await connection.ExecuteAsync(Command("""
                INSERT dbo.ChatMessages (MessageId, SessionId, RequestId, Sequence, Role, Content, Status, AttemptId, LeaseUntil, AttemptsJson)
                VALUES (NEWID(), @SessionId, @RequestId, @Sequence, 'user', @Message, 'Pending', @AttemptId, DATEADD(second, 180, SYSUTCDATETIME()), @Attempts);
                UPDATE dbo.ChatSessions SET NextSequence = NextSequence + 1, UpdatedAt = SYSUTCDATETIME() WHERE SessionId = @SessionId;
                """, new { request.SessionId, request.RequestId, existing.Sequence, request.Message, AttemptId = attemptId,
                    Attempts = JsonSerializer.Serialize(attempts, ChatPolicy.Json) }, transaction, cancellationToken));
        }
        else
        {
            await connection.ExecuteAsync(Command("""
                UPDATE dbo.ChatMessages SET Status = 'Pending', AttemptId = @AttemptId,
                    LeaseUntil = DATEADD(second, 180, SYSUTCDATETIME()), AttemptsJson = @Attempts
                WHERE SessionId = @SessionId AND RequestId = @RequestId AND Role = 'user';
                """, new { request.SessionId, request.RequestId, AttemptId = attemptId,
                    Attempts = JsonSerializer.Serialize(attempts, ChatPolicy.Json) }, transaction, cancellationToken));
        }
        await transaction.CommitAsync(cancellationToken);
        return new PendingTurn(attemptId, messages.Select(ToTranscript).ToArray(), null);
    }

    public async Task<IReadOnlyList<FaqEntry>> GetFaqAsync(CancellationToken cancellationToken)
    {
        await using var connection = await database.OpenAsync(cancellationToken);
        return (await connection.QueryAsync<FaqEntry>(new CommandDefinition(
            "SELECT Id, Topic, Question, Answer, Source FROM dbo.FaqEntries ORDER BY Id;", cancellationToken: cancellationToken))).AsList();
    }

    public async Task<TurnResult> CompleteTurnAsync(TurnRequest request, Guid attemptId, ModelReply reply, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(reply.Text) || reply.Text.Length > ChatPolicy.MaxReplyCharacters)
            throw new ChatProblem(502, "reply_size", "The reply could not be saved within the chat limit. Retry this message.");
        await using var connection = await database.OpenAsync(cancellationToken);
        await using var transaction = (SqlTransaction)await connection.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);
        var session = await LockSession(connection, transaction, request.SessionId, cancellationToken)
            ?? throw new ChatProblem(404, "session", "Chat not found.");
        var messages = await ReadMessages(connection, transaction, request.SessionId, cancellationToken);
        var visitor = messages.Single(message => message.RequestId == request.RequestId && message.Role == "user");
        if (visitor.Status == "Completed")
        {
            await transaction.CommitAsync(cancellationToken);
            return Result(request, messages.Single(message => message.RequestId == request.RequestId && message.Role == "assistant"));
        }
        if (visitor.AttemptId != attemptId || visitor.Status != "Pending" || session.State != "Open")
            throw new ChatProblem(409, "attempt_expired", "This attempt is no longer active. Retry the original message.");
        var now = DateTime.UtcNow;
        visitor.Status = "Completed";
        var response = new MessageRow
        {
            Sequence = session.NextSequence, RequestId = request.RequestId, Role = "assistant",
            Content = reply.Text, CreatedAt = now, Status = "Completed",
            SourcesJson = JsonSerializer.Serialize(reply.Sources, ChatPolicy.Json)
        };
        messages.Add(response);
        ChatPolicy.SerializeSnapshot(request.SessionId, messages.Select(ToTranscript).ToArray());
        var attempts = Deserialize<List<AttemptRecord>>(visitor.AttemptsJson);
        CloseAttempt(attempts, now, "Completed", null);
        await connection.ExecuteAsync(Command("""
            INSERT dbo.ChatMessages (MessageId, SessionId, RequestId, Sequence, Role, Content, Status, SourcesJson, Model, InputTokens, OutputTokens, FinishReason)
            VALUES (NEWID(), @SessionId, @RequestId, @Sequence, 'assistant', @Text, 'Completed', @SourcesJson, @Model, @InputTokens, @OutputTokens, @FinishReason);
            UPDATE dbo.ChatMessages SET Status = 'Completed', LeaseUntil = NULL, AttemptsJson = @Attempts
            WHERE SessionId = @SessionId AND RequestId = @RequestId AND Role = 'user';
            UPDATE dbo.ChatSessions SET NextSequence = NextSequence + 1, UpdatedAt = SYSUTCDATETIME() WHERE SessionId = @SessionId;
            """, new { request.SessionId, request.RequestId, response.Sequence, reply.Text, response.SourcesJson,
                reply.Model, reply.InputTokens, reply.OutputTokens, reply.FinishReason,
                Attempts = JsonSerializer.Serialize(attempts, ChatPolicy.Json) }, transaction, cancellationToken));
        await transaction.CommitAsync(cancellationToken);
        return Result(request, response);
    }

    public async Task FailTurnAsync(TurnRequest request, Guid attemptId, string errorCode, CancellationToken cancellationToken)
    {
        await using var connection = await database.OpenAsync(cancellationToken);
        await using var transaction = (SqlTransaction)await connection.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);
        await LockSession(connection, transaction, request.SessionId, cancellationToken);
        var message = await connection.QuerySingleOrDefaultAsync<MessageRow>(Command("""
            SELECT * FROM dbo.ChatMessages WHERE SessionId = @SessionId AND RequestId = @RequestId AND Role = 'user';
            """, request, transaction, cancellationToken));
        if (message?.AttemptId == attemptId && message.Status == "Pending")
        {
            var attempts = Deserialize<List<AttemptRecord>>(message.AttemptsJson);
            CloseAttempt(attempts, DateTime.UtcNow, "Failed", errorCode);
            await connection.ExecuteAsync(Command("""
                UPDATE dbo.ChatMessages SET Status = 'Failed', LeaseUntil = NULL, AttemptsJson = @Attempts
                WHERE SessionId = @SessionId AND RequestId = @RequestId AND Role = 'user';
                UPDATE dbo.ChatSessions SET UpdatedAt = SYSUTCDATETIME() WHERE SessionId = @SessionId;
                """, new { request.SessionId, request.RequestId, Attempts = JsonSerializer.Serialize(attempts, ChatPolicy.Json) }, transaction, cancellationToken));
        }
        await transaction.CommitAsync(cancellationToken);
    }

    public async Task<EscalationResult> EscalateAsync(EscalationRequest request, byte[] ownerHash, CancellationToken cancellationToken)
    {
        var email = ChatPolicy.ValidateEmail(request.Email);
        if (request.SessionId == Guid.Empty || request.RequestId == Guid.Empty)
            throw new ChatProblem(400, "request", "Start a chat before requesting follow-up.");
        await using var connection = await database.OpenAsync(cancellationToken);
        await using var transaction = (SqlTransaction)await connection.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);
        var session = await LockSession(connection, transaction, request.SessionId, cancellationToken)
            ?? throw new ChatProblem(404, "session", "Start a chat before requesting follow-up.");
        VerifyOwner(session, ownerHash);
        var existing = await connection.QuerySingleOrDefaultAsync<EscalationResult>(Command("""
            SELECT EscalationId, SessionId, CreatedAt FROM dbo.ChatEscalations WHERE SessionId = @SessionId;
            """, request, transaction, cancellationToken));
        if (existing is not null)
        {
            await transaction.CommitAsync(cancellationToken);
            return existing;
        }
        var messages = await ReadMessages(connection, transaction, request.SessionId, cancellationToken);
        if (messages.Count == 0)
            throw new ChatProblem(409, "empty", "Send a message before requesting follow-up.");
        var now = await connection.QuerySingleAsync<DateTime>(Command("SELECT SYSUTCDATETIME();", null, transaction, cancellationToken));
        foreach (var message in messages.Where(message => message.Status == "Pending"))
        {
            if (message.LeaseUntil > now)
                throw new ChatProblem(409, "pending", "Wait for the current reply before requesting follow-up.");
            var attempts = Deserialize<List<AttemptRecord>>(message.AttemptsJson);
            CloseAttempt(attempts, now, "Interrupted", "lease_expired");
            message.Status = "Failed";
            await connection.ExecuteAsync(Command("""
                UPDATE dbo.ChatMessages SET Status = 'Failed', LeaseUntil = NULL, AttemptsJson = @Attempts
                WHERE SessionId = @SessionId AND RequestId = @RequestId AND Role = 'user';
                """, new { request.SessionId, message.RequestId, Attempts = JsonSerializer.Serialize(attempts, ChatPolicy.Json) }, transaction, cancellationToken));
        }
        var transcript = ChatPolicy.SerializeSnapshot(request.SessionId, messages.Select(ToTranscript).ToArray());
        var result = await connection.QuerySingleAsync<EscalationResult>(Command("""
            INSERT dbo.ChatEscalations (EscalationId, SessionId, RequestId, Email, TranscriptJson)
            OUTPUT inserted.EscalationId, inserted.SessionId, inserted.CreatedAt
            VALUES (NEWID(), @SessionId, @RequestId, @Email, @Transcript);
            UPDATE dbo.ChatSessions SET State = 'Escalated', UpdatedAt = SYSUTCDATETIME() WHERE SessionId = @SessionId;
            """, new { request.SessionId, request.RequestId, Email = email, Transcript = transcript }, transaction, cancellationToken));
        await transaction.CommitAsync(cancellationToken);
        return result;
    }

    private static CommandDefinition Command(string sql, object? parameters, SqlTransaction transaction, CancellationToken token) =>
        new(sql, parameters, transaction, commandTimeout: 30, cancellationToken: token);

    private static Task<SessionRow?> LockSession(SqlConnection connection, SqlTransaction transaction, Guid sessionId, CancellationToken token) =>
        connection.QuerySingleOrDefaultAsync<SessionRow>(Command(
            "SELECT * FROM dbo.ChatSessions WITH (UPDLOCK, HOLDLOCK) WHERE SessionId = @SessionId;",
            new { SessionId = sessionId }, transaction, token));

    private static async Task<List<MessageRow>> ReadMessages(SqlConnection connection, SqlTransaction transaction, Guid sessionId, CancellationToken token) =>
        (await connection.QueryAsync<MessageRow>(Command(
            "SELECT * FROM dbo.ChatMessages WHERE SessionId = @SessionId ORDER BY Sequence;",
            new { SessionId = sessionId }, transaction, token))).AsList();

    private static void VerifyOwner(SessionRow session, byte[] ownerHash)
    {
        if (!CryptographicOperations.FixedTimeEquals(session.OwnerHash, ownerHash))
            throw new ChatProblem(404, "session", "This chat is not accessible. Start a new chat.");
    }

    private static T Deserialize<T>(string json) => JsonSerializer.Deserialize<T>(json, ChatPolicy.Json)!;
    private static TranscriptMessage ToTranscript(MessageRow message) => new(message.Sequence, message.RequestId,
        message.Role, message.Content, DateTime.SpecifyKind(message.CreatedAt, DateTimeKind.Utc), message.Status,
        Deserialize<List<SourceReference>>(message.SourcesJson));
    private static TurnResult Result(TurnRequest request, MessageRow message) =>
        new(request.SessionId, request.RequestId, message.Content, Deserialize<List<SourceReference>>(message.SourcesJson));

    private static void CloseAttempt(List<AttemptRecord> attempts, DateTime now, string status, string? error)
    {
        if (attempts.Count > 0 && attempts[^1].EndedAt is null)
            attempts[^1] = attempts[^1] with { EndedAt = now, Status = status, Error = error };
    }

    private sealed record AttemptRecord(Guid AttemptId, DateTime StartedAt, DateTime? EndedAt, string Status, string? Error);
    private sealed class SessionRow
    {
        public Guid SessionId { get; set; }
        public byte[] OwnerHash { get; set; } = [];
        public string State { get; set; } = "Open";
        public int NextSequence { get; set; } = 1;
    }
    private sealed class MessageRow
    {
        public Guid RequestId { get; set; }
        public int Sequence { get; set; }
        public string Role { get; set; } = "";
        public string Content { get; set; } = "";
        public DateTime CreatedAt { get; set; }
        public string Status { get; set; } = "";
        public string SourcesJson { get; set; } = "[]";
        public Guid? AttemptId { get; set; }
        public DateTime? LeaseUntil { get; set; }
        public string AttemptsJson { get; set; } = "[]";
    }
}