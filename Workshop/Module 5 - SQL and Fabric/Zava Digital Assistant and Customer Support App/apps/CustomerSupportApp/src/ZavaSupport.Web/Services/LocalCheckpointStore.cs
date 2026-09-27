using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Azure.Messaging.EventHubs.Consumer;

namespace ZavaSupport.Web.Services;

public sealed class LocalCheckpointStore
{
    private readonly string filePath;

    public LocalCheckpointStore(string directory, string fullyQualifiedNamespace, string hubName, string consumerGroup)
    {
        var identity = JsonSerializer.Serialize(new[] { fullyQualifiedNamespace.ToLowerInvariant(), hubName, consumerGroup });
        var fileName = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(identity))) + ".json";
        filePath = Path.Combine(directory, fileName);
    }

    public async Task<Dictionary<string, long>> LoadAsync(CancellationToken cancellationToken)
    {
        try
        {
            await using var stream = File.OpenRead(filePath);
            var positions = await JsonSerializer.DeserializeAsync<Dictionary<string, long>>(stream, cancellationToken: cancellationToken)
                ?? throw new InvalidDataException("Checkpoint file is empty.");
            if (positions.Any(position => position.Value < 0))
                throw new InvalidDataException("Checkpoint sequence numbers must not be negative.");
            return positions;
        }
        catch (FileNotFoundException)
        {
            return [];
        }
        catch (DirectoryNotFoundException)
        {
            return [];
        }
    }

    public static EventPosition GetStartPosition(IReadOnlyDictionary<string, long> positions, string partitionId) =>
        positions.TryGetValue(partitionId, out var sequenceNumber)
            ? EventPosition.FromSequenceNumber(sequenceNumber, isInclusive: false)
            : EventPosition.Earliest;

    public async Task SaveAsync(Dictionary<string, long> positions, CancellationToken cancellationToken)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);
        var temporaryPath = filePath + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try
        {
            await using (var stream = new FileStream(temporaryPath, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            {
                await JsonSerializer.SerializeAsync(stream, positions, cancellationToken: cancellationToken);
                await stream.FlushAsync(cancellationToken);
                stream.Flush(flushToDisk: true);
            }
            cancellationToken.ThrowIfCancellationRequested();
            File.Move(temporaryPath, filePath, overwrite: true);
        }
        finally
        {
            File.Delete(temporaryPath);
        }
    }
}