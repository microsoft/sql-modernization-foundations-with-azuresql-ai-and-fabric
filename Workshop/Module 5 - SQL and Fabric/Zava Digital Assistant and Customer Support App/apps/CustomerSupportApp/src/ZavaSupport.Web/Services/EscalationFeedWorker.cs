using System.Text.Json;
using Azure.Identity;
using Azure.Messaging.EventHubs;
using Azure.Messaging.EventHubs.Consumer;

namespace ZavaSupport.Web.Services;

/// Reads the escalation hub continuously and feeds the in-memory store. It never writes to the hub or to SQL.
public sealed class EscalationFeedWorker(
    EventHubSettings settings,
    EscalationStore store,
    IWebHostEnvironment environment,
    ILogger<EscalationFeedWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (settings.DescribeConfigurationProblem() is { } configurationProblem)
        {
            store.SetStatus("error", configurationProblem);
            return;
        }
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                store.SetStatus("connecting", $"Connecting to {settings.EffectiveHubName} as {settings.ConsumerGroup}.");
                var options = new EventHubConsumerClientOptions
                {
                    ConnectionOptions = new EventHubConnectionOptions { TransportType = EventHubsTransportType.AmqpWebSockets }
                };
                await using var consumer = CreateConsumer(options);
                var checkpoints = new LocalCheckpointStore(
                    Path.Combine(environment.ContentRootPath, "App_Data", "checkpoints"),
                    consumer.FullyQualifiedNamespace, consumer.EventHubName, settings.ConsumerGroup);
                var savedPositions = await checkpoints.LoadAsync(stoppingToken);
                var positions = new Dictionary<string, long>(savedPositions);
                var partitionIds = await consumer.GetPartitionIdsAsync(stoppingToken);
                var reader = new CesEscalationReader();
                using var connectionCancellation = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken);
                using var processingGate = new SemaphoreSlim(1, 1);
                store.SetStatus("live", $"Streaming {settings.EffectiveHubName} as {settings.ConsumerGroup}.");
                await Task.WhenAll(partitionIds.Select(ReadPartitionAsync));

                async Task ReadPartitionAsync(string partitionId)
                {
                    try
                    {
                        var token = connectionCancellation.Token;
                        await foreach (var received in consumer.ReadEventsFromPartitionAsync(partitionId,
                            LocalCheckpointStore.GetStartPosition(savedPositions, partitionId),
                            new ReadEventOptions { MaximumWaitTime = TimeSpan.FromSeconds(30) }, token))
                        {
                            if (received.Data is null) continue;
                            await processingGate.WaitAsync(token);
                            try
                            {
                                try
                                {
                                    if (reader.Read(received.Data.EventBody.ToString()) is { } escalation) store.Publish(escalation);
                                }
                                catch (Exception error) when (error is JsonException or InvalidDataException)
                                {
                                    logger.LogWarning("Skipped an event that is not a readable CES escalation insert ({Error}).", error.GetType().Name);
                                }
                                positions[partitionId] = received.Data.SequenceNumber;
                                if (!reader.HasPendingSegments)
                                    await checkpoints.SaveAsync(positions, token);
                            }
                            finally
                            {
                                processingGate.Release();
                            }
                        }
                    }
                    catch
                    {
                        await connectionCancellation.CancelAsync();
                        throw;
                    }
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception error)
            {
                if (settings.UsesConnectionString)
                    logger.LogError("The escalation stream stopped ({Error}). Check the Listen policy, connection string, local authentication and consumer group.", error.GetType().Name);
                else
                    logger.LogError(error, "The escalation stream stopped.");
                var guidance = settings.UsesConnectionString
                    ? "the connection string, policy Listen permission, namespace local authentication and consumer group"
                    : "az login, the Data Receiver role and the consumer group";
                if (error is IOException or UnauthorizedAccessException or JsonException)
                    guidance = "the local App_Data/checkpoints files and directory permissions";
                store.SetStatus("error", $"Stream unavailable ({error.GetType().Name}). Check {guidance}. Retrying.");
                try
                {
                    await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
            }
        }
    }

    private EventHubConsumerClient CreateConsumer(EventHubConsumerClientOptions options)
    {
        if (!settings.UsesConnectionString)
            return new EventHubConsumerClient(settings.ConsumerGroup, settings.Namespace, settings.HubName, new DefaultAzureCredential(), options);

        // Fabric Event Stream connection strings embed the hub name as EntityPath. Passing it again as a
        // separate argument makes the SDK throw ArgumentException, so only supply it when it is missing.
        return settings.ConnectionStringHubName is not null
            ? new EventHubConsumerClient(settings.ConsumerGroup, settings.ConnectionString, options)
            : new EventHubConsumerClient(settings.ConsumerGroup, settings.ConnectionString, settings.HubName, options);
    }
}
