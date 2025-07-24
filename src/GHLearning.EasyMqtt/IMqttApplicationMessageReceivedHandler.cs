using MQTTnet.Client;

namespace GHLearning.EasyMqtt;

public interface IMqttApplicationMessageReceivedHandler
{
    Task HandleMessageAsync(MqttApplicationMessageReceivedEventArgs arg);
}