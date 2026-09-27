using Azure.Messaging.EventHubs;

namespace ZavaSupport.Web.Services;

public sealed record EventHubSettings(string? Namespace, string? HubName, string ConsumerGroup, string? ConnectionString = null)
{
    public bool UsesConnectionString => !string.IsNullOrWhiteSpace(ConnectionString);

    /// The hub name embedded in the connection string (EntityPath), which Fabric Event Stream always supplies.
    public string? ConnectionStringHubName
    {
        get
        {
            if (!UsesConnectionString) return null;
            try
            {
                var entityPath = EventHubsConnectionStringProperties.Parse(ConnectionString).EventHubName;
                return string.IsNullOrWhiteSpace(entityPath) ? null : entityPath;
            }
            catch (FormatException)
            {
                return null;
            }
        }
    }

    /// The hub name to connect to, preferring the connection string's EntityPath over the standalone setting.
    public string? EffectiveHubName => ConnectionStringHubName ?? (string.IsNullOrWhiteSpace(HubName) ? null : HubName);

    public bool IsConfigured => EffectiveHubName is not null && (UsesConnectionString || !string.IsNullOrWhiteSpace(Namespace));

    /// Returns a human-readable reason the settings cannot open a stream, or null when they are usable.
    public string? DescribeConfigurationProblem()
    {
        if (!UsesConnectionString)
            return IsConfigured
                ? null
                : "Set ZAVA_EVENTHUB_NAME and either ZAVA_EVENTHUB_CONNECTION_STRING or ZAVA_EVENTHUB_NAMESPACE, then restart the console.";

        EventHubsConnectionStringProperties properties;
        try
        {
            properties = EventHubsConnectionStringProperties.Parse(ConnectionString);
        }
        catch (FormatException)
        {
            return "ZAVA_EVENTHUB_CONNECTION_STRING is malformed. Copy the whole value, e.g. Endpoint=sb://<namespace>.servicebus.windows.net/;SharedAccessKeyName=Listen;SharedAccessKey=<key>;EntityPath=<hub>.";
        }

        if (string.IsNullOrEmpty(properties.Endpoint?.Host))
            return "ZAVA_EVENTHUB_CONNECTION_STRING is missing the Endpoint host. Copy the full connection string, not just the key.";
        if (string.IsNullOrEmpty(properties.SharedAccessSignature) &&
            (string.IsNullOrEmpty(properties.SharedAccessKeyName) || string.IsNullOrEmpty(properties.SharedAccessKey)))
            return "ZAVA_EVENTHUB_CONNECTION_STRING is missing SharedAccessKeyName/SharedAccessKey (or a SharedAccessSignature). Use the Listen policy's connection string.";
        if (EffectiveHubName is null)
            return "ZAVA_EVENTHUB_CONNECTION_STRING has no EntityPath, so also set ZAVA_EVENTHUB_NAME to the hub name.";
        return null;
    }

    public static EventHubSettings Read(IConfiguration configuration) => new(
        configuration["EventHub:Namespace"] ?? configuration["ZAVA_EVENTHUB_NAMESPACE"],
        configuration["EventHub:Name"] ?? configuration["ZAVA_EVENTHUB_NAME"],
        configuration["EventHub:ConsumerGroup"] ?? configuration["ZAVA_EVENTHUB_CONSUMER_GROUP"] ?? "$Default",
        configuration["EventHub:ConnectionString"] ?? configuration["ZAVA_EVENTHUB_CONNECTION_STRING"]);
}
