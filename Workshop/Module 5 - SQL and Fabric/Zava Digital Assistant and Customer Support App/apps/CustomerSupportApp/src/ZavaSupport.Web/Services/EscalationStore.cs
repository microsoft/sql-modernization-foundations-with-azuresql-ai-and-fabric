using System.Threading.Channels;
using ZavaSupport.Web.Models;

namespace ZavaSupport.Web.Services;

/// In-memory, read-only view of escalations received from the hub during this process lifetime.
public sealed class EscalationStore
{
    private const int Capacity = 500;

    private readonly object gate = new();
    private readonly List<Escalation> recent = [];
    private readonly HashSet<Guid> seen = [];
    private readonly List<Channel<FeedEvent>> subscribers = [];
    private FeedStatus status = new("connecting", "Connecting to the escalation stream.");

    public void Publish(Escalation escalation)
    {
        lock (gate)
        {
            if (!seen.Add(escalation.EscalationId)) return;
            recent.Insert(0, escalation);
            if (recent.Count > Capacity)
            {
                seen.Remove(recent[^1].EscalationId);
                recent.RemoveAt(recent.Count - 1);
            }
            Broadcast(new FeedEvent("escalation", escalation));
        }
    }

    public void SetStatus(string state, string message)
    {
        lock (gate)
        {
            if (status.State == state && status.Message == message) return;
            status = new FeedStatus(state, message);
            Broadcast(new FeedEvent("status", status));
        }
    }

    public Subscription Subscribe()
    {
        var channel = Channel.CreateBounded<FeedEvent>(new BoundedChannelOptions(256)
        {
            FullMode = BoundedChannelFullMode.DropOldest, SingleReader = true
        });
        lock (gate)
        {
            subscribers.Add(channel);
            channel.Writer.TryWrite(new FeedEvent("snapshot", new { escalations = recent.ToArray(), status }));
        }
        return new Subscription(this, channel);
    }

    private void Broadcast(FeedEvent message)
    {
        foreach (var subscriber in subscribers) subscriber.Writer.TryWrite(message);
    }

    private void Unsubscribe(Channel<FeedEvent> channel)
    {
        lock (gate) subscribers.Remove(channel);
        channel.Writer.TryComplete();
    }

    public sealed class Subscription(EscalationStore store, Channel<FeedEvent> channel) : IDisposable
    {
        public ChannelReader<FeedEvent> Reader => channel.Reader;

        public void Dispose() => store.Unsubscribe(channel);
    }
}
