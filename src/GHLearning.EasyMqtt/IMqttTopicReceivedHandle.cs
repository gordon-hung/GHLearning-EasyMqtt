namespace GHLearning.EasyMqtt;

public interface IMqttTopicReceivedHandle
{
    Task ReceivedHandledAsync(IReadOnlyCollection<byte> bytes, CancellationToken cancellationToken = default);
}