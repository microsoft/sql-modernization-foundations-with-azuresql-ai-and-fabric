using Azure.Messaging.EventHubs.Consumer;
using Xunit;
using ZavaSupport.Web.Services;

namespace ZavaSupport.Web.Tests;

public sealed class LocalCheckpointStoreTests : IDisposable
{
    private readonly string directory = Path.Combine(Path.GetTempPath(), "zava-checkpoints-" + Guid.NewGuid().ToString("N"));

    private LocalCheckpointStore CreateStore(string host = "demo.servicebus.windows.net", string hub = "events", string group = "support") =>
        new(directory, host, hub, group);

    [Fact]
    public async Task MissingCheckpointStartsAtEarliest()
    {
        var positions = await CreateStore().LoadAsync(CancellationToken.None);

        Assert.Empty(positions);
        Assert.Equal(EventPosition.Earliest, LocalCheckpointStore.GetStartPosition(positions, "0"));
    }

    [Fact]
    public async Task RestartResumesAfterEachSavedPartitionPosition()
    {
        await CreateStore().SaveAsync(new() { ["0"] = 12, ["1"] = 34 }, CancellationToken.None);

        var positions = await CreateStore().LoadAsync(CancellationToken.None);

        Assert.Equal(12, positions["0"]);
        Assert.Equal(34, positions["1"]);
        Assert.Equal(EventPosition.FromSequenceNumber(12, isInclusive: false), LocalCheckpointStore.GetStartPosition(positions, "0"));
        Assert.Equal(EventPosition.Earliest, LocalCheckpointStore.GetStartPosition(positions, "2"));
    }

    [Fact]
    public async Task ReplacementPreservesLatestPositionsAndLeavesNoTemporaryFiles()
    {
        var store = CreateStore();
        await store.SaveAsync(new() { ["0"] = 12 }, CancellationToken.None);
        await store.SaveAsync(new() { ["0"] = 13, ["1"] = 34 }, CancellationToken.None);

        Assert.Equal(13, (await CreateStore().LoadAsync(CancellationToken.None))["0"]);
        Assert.Single(Directory.GetFiles(directory));
    }

    [Theory]
    [InlineData("other.servicebus.windows.net", "events", "support")]
    [InlineData("demo.servicebus.windows.net", "other", "support")]
    [InlineData("demo.servicebus.windows.net", "events", "other")]
    public async Task DifferentStreamsHaveIndependentCheckpoints(string host, string hub, string group)
    {
        await CreateStore().SaveAsync(new() { ["0"] = 12 }, CancellationToken.None);

        Assert.Empty(await CreateStore(host, hub, group).LoadAsync(CancellationToken.None));
    }

    [Fact]
    public async Task CancelledSavePreservesPreviousCheckpoint()
    {
        var store = CreateStore();
        await store.SaveAsync(new() { ["0"] = 12 }, CancellationToken.None);
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => store.SaveAsync(new() { ["0"] = 13 }, cancellation.Token));

        Assert.Equal(12, (await CreateStore().LoadAsync(CancellationToken.None))["0"]);
        Assert.Single(Directory.GetFiles(directory));
    }

    [Fact]
    public async Task InvalidCheckpointDoesNotSilentlyResetProgress()
    {
        await CreateStore().SaveAsync(new() { ["0"] = -1 }, CancellationToken.None);

        await Assert.ThrowsAsync<InvalidDataException>(() => CreateStore().LoadAsync(CancellationToken.None));
    }

    public void Dispose()
    {
        if (Directory.Exists(directory)) Directory.Delete(directory, recursive: true);
    }
}