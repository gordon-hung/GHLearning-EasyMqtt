using System.Text;

namespace GHLearning.EasyMqtt.CollectorCenter.ReceivedHandles;

internal sealed class TemperatureReceivedHandle : IMqttTopicReceivedHandle
{
    public Task ReceivedHandledAsync(IReadOnlyCollection<byte> bytes, CancellationToken cancellationToken = default)
    {
        var payloadText = Encoding.UTF8.GetString([.. bytes]);

        Console.WriteLine($"""
              Handle:{nameof(TemperatureReceivedHandle)}
              Payload:{payloadText}
            """);

        return Task.CompletedTask;
    }
}