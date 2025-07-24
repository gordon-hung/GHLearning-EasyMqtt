namespace GHLearning.EasyMqtt;

public interface IMqttClient
{
    Task StopAsync();

    Task StartAsync();

    Task EnqueueAsync(string topic, string payload);
}