namespace GHLearning.EasyMqtt;

public interface IMqttMessage
{
    Task EnqueueAsync(string topic, string payload, CancellationToken cancellationToken = default);
}