using System.Globalization;
using System.Text.Json;
using System.Text.Json.Nodes;
using Xunit;
using ZavaSupport.Web.Services;

namespace ZavaSupport.Web.Tests;

public sealed class CesEscalationReaderTests
{
    private static string Sample => File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Fixtures", "chat-escalation.json"));

    [Fact]
    public void ReadsSampleAndSerializesForFrontend()
    {
        var escalation = new CesEscalationReader().Read(Sample);

        Assert.NotNull(escalation);
        Assert.Equal(Guid.Parse("6E10C210-20ED-469F-BEEE-D022DFAD13C4"), escalation.EscalationId);
        Assert.Equal(Guid.Parse("C73761F8-6700-47A0-82A0-94658370D968"), escalation.SessionId);
        Assert.Equal("alex@example.com", escalation.Email);
        Assert.Equal(DateTimeKind.Utc, escalation.CreatedAt.Kind);
        Assert.Equal("2026-09-17T16:01:05.5746966Z", escalation.CreatedAt.ToString("O"));
        Assert.Equal(2, escalation.MessageCount);
        Assert.Collection(escalation.Messages,
            message =>
            {
                Assert.Equal(1, message.Sequence);
                Assert.Equal("user", message.Role);
                Assert.Equal("escalate", message.Content);
                Assert.Equal("Completed", message.Status);
            },
            message =>
            {
                Assert.Equal(2, message.Sequence);
                Assert.Equal("assistant", message.Role);
                Assert.StartsWith("Choose Chat with a real person", message.Content);
                Assert.Equal("2026-09-17T16:00:54.8296062Z", message.CreatedAt.ToString("O"));
            });

        using var payload = JsonDocument.Parse(JsonSerializer.Serialize(escalation, new JsonSerializerOptions(JsonSerializerDefaults.Web)));
        Assert.Equal("alex@example.com", payload.RootElement.GetProperty("email").GetString());
        Assert.Equal(2, payload.RootElement.GetProperty("messageCount").GetInt32());
        Assert.Equal("escalate", payload.RootElement.GetProperty("messages")[0].GetProperty("content").GetString());
    }

    [Theory]
    [InlineData("application/json")]
    [InlineData("application/avro-json")]
    public void ReadsJsonWithIsoTimestamp(string contentType)
    {
        var envelope = JsonNode.Parse(Sample)!;
        envelope["datacontenttype"] = contentType;
        SetCreatedAt(envelope, "2026-09-17T16:01:05.5746966Z");

        Assert.NotNull(new CesEscalationReader().Read(envelope.ToJsonString()));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ReassemblesOneBasedSegmentsIncludingOutOfOrderDelivery(bool finalFirst)
    {
        var first = JsonNode.Parse(Sample)!;
        var last = first.DeepClone();
        var data = first["data"]!.GetValue<string>();
        var midpoint = data.Length / 2;
        first["finalsegment"] = false;
        first["data"] = data[..midpoint];
        last["id"] = Guid.NewGuid().ToString();
        last["segmentindex"] = 2;
        last["data"] = data[midpoint..];
        var reader = new CesEscalationReader();

        Assert.False(reader.HasPendingSegments);
        Assert.Null(reader.Read((finalFirst ? last : first).ToJsonString()));
        Assert.True(reader.HasPendingSegments);
        var escalation = reader.Read((finalFirst ? first : last).ToJsonString());

        Assert.NotNull(escalation);
        Assert.False(reader.HasPendingSegments);
        Assert.Equal("alex@example.com", escalation.Email);
        Assert.Equal(2, escalation.MessageCount);
    }

    [Fact]
    public void IgnoresDuplicateEvent()
    {
        var reader = new CesEscalationReader();
        Assert.NotNull(reader.Read(Sample));
        Assert.Null(reader.Read(Sample));
    }

    [Fact]
    public void ParsesSqlTimestampIndependentlyOfCurrentCulture()
    {
        var originalCulture = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("fr-FR");
            Assert.Equal("2026-09-17T16:01:05.5746966Z", new CesEscalationReader().Read(Sample)!.CreatedAt.ToString("O"));
        }
        finally
        {
            CultureInfo.CurrentCulture = originalCulture;
        }
    }

    [Fact]
    public void RejectsInvalidSqlTimestampAsUnreadableData()
    {
        var envelope = JsonNode.Parse(Sample)!;
        SetCreatedAt(envelope, "not-a-date");

        Assert.Throws<InvalidDataException>(() => new CesEscalationReader().Read(envelope.ToJsonString()));
    }

    [Fact]
    public void RejectsUnsupportedEncoding()
    {
        var envelope = JsonNode.Parse(Sample)!;
        envelope["datacontenttype"] = "application/avro";

        Assert.Throws<InvalidDataException>(() => new CesEscalationReader().Read(envelope.ToJsonString()));
    }

    private static void SetCreatedAt(JsonNode envelope, string timestamp)
    {
        var data = JsonNode.Parse(envelope["data"]!.GetValue<string>())!;
        var row = JsonNode.Parse(data["eventrow"]!["current"]!.GetValue<string>())!;
        row["CreatedAt"] = timestamp;
        data["eventrow"]!["current"] = row.ToJsonString();
        envelope["data"] = data.ToJsonString();
    }
}