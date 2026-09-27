using System.Globalization;
using System.Text.Json;
using ZavaSupport.Web.Models;

namespace ZavaSupport.Web.Services;

/// Decodes the native Fabric SQL CES CloudEvents envelope that carries dbo.ChatEscalations inserts.
public sealed class CesEscalationReader
{
    private readonly HashSet<string> seenEvents = [];
    private readonly Dictionary<string, Segments> pending = [];
    private int bufferedCharacters;

    public bool HasPendingSegments => pending.Count > 0;

    public Escalation? Read(string envelope)
    {
        using var document = JsonDocument.Parse(envelope);
        var root = document.RootElement;
        if (root.GetProperty("type").GetString() != "com.microsoft.SQL.CES.DML.V1" ||
            root.GetProperty("operation").GetString() != "INS") return null;
        if (root.GetProperty("datacontenttype").GetString() is not ("application/json" or "application/avro-json"))
            throw new InvalidDataException("This console requires CES JSON or Avro JSON encoding.");
        var eventKey = root.GetProperty("source").GetString() + ":" + root.GetProperty("id").GetString();
        if (!seenEvents.Add(eventKey)) return null;
        if (seenEvents.Count > 10000) throw new InvalidDataException("Event budget reached for this stream connection.");
        var logicalId = root.GetProperty("logicalid").GetString()!;
        var segmentIndex = root.GetProperty("segmentindex").GetInt32();
        if (segmentIndex < 1 || segmentIndex > 100) throw new InvalidDataException("Invalid CES segment index.");
        if (!pending.TryGetValue(logicalId, out var segments))
        {
            if (pending.Count >= 64) throw new InvalidDataException("Too many incomplete CES messages.");
            pending[logicalId] = segments = new Segments();
        }
        var fragment = root.GetProperty("data").GetString() ?? throw new InvalidDataException("Missing JSON event data.");
        if (segments.Parts.TryGetValue(segmentIndex, out var prior))
        {
            if (prior != fragment) throw new InvalidDataException("Conflicting duplicate CES segment.");
        }
        else
        {
            segments.Parts.Add(segmentIndex, fragment);
            bufferedCharacters += fragment.Length;
            if (bufferedCharacters > 2 * 1024 * 1024) throw new InvalidDataException("CES buffer limit exceeded.");
        }
        if (root.GetProperty("finalsegment").GetBoolean()) segments.Last = segmentIndex;
        if (segments.Last is null || !Enumerable.Range(1, segments.Last.Value).All(segments.Parts.ContainsKey)) return null;
        var payload = string.Concat(segments.Parts.OrderBy(part => part.Key).Select(part => part.Value));
        bufferedCharacters -= segments.Parts.Sum(part => part.Value.Length);
        pending.Remove(logicalId);
        using var data = JsonDocument.Parse(payload);
        var source = data.RootElement.GetProperty("eventsource");
        if (source.GetProperty("schema").GetString() != "dbo" || source.GetProperty("tbl").GetString() != "ChatEscalations") return null;
        using var row = JsonDocument.Parse(data.RootElement.GetProperty("eventrow").GetProperty("current").GetString()!);
        var escalationId = row.RootElement.GetProperty("EscalationId").GetGuid();
        var sessionId = row.RootElement.GetProperty("SessionId").GetGuid();
        var email = row.RootElement.GetProperty("Email").GetString() ?? "";
        var timestamp = row.RootElement.GetProperty("CreatedAt");
        if (!timestamp.TryGetDateTime(out var createdAt) &&
            !DateTime.TryParseExact(timestamp.GetString(), "yyyy-MM-dd HH:mm:ss.FFFFFFF", CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out createdAt))
            throw new InvalidDataException("Invalid escalation CreatedAt timestamp.");
        using var snapshot = JsonDocument.Parse(row.RootElement.GetProperty("TranscriptJson").GetString()!);
        if (snapshot.RootElement.GetProperty("version").GetInt32() != 1 || snapshot.RootElement.GetProperty("sessionId").GetGuid() != sessionId)
            throw new InvalidDataException("Unexpected transcript version or session.");
        var messages = snapshot.RootElement.GetProperty("messages").EnumerateArray().Select(message => new TranscriptMessage(
            message.GetProperty("sequence").GetInt32(),
            message.GetProperty("role").GetString() ?? "",
            message.GetProperty("content").GetString() ?? "",
            message.GetProperty("createdAt").GetDateTime(),
            message.GetProperty("status").GetString() ?? "")).OrderBy(message => message.Sequence).ToArray();
        if (messages.Length == 0) throw new InvalidDataException("Transcript messages are missing.");
        return new Escalation(escalationId, sessionId, email, createdAt, DateTime.UtcNow, messages.Length, messages);
    }

    private sealed class Segments
    {
        public SortedDictionary<int, string> Parts { get; } = [];
        public int? Last { get; set; }
    }
}
