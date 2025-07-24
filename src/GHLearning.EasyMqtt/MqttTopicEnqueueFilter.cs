using MQTTnet.Protocol;

namespace GHLearning.EasyMqtt;

public sealed class MqttTopicEnqueueFilter
{
    public string Topic { get; set; } = default!;
    public MqttQualityOfServiceLevel QualityOfServiceLevel { get; set; } = MqttQualityOfServiceLevel.AtMostOnce;
    public bool Retain { get; set; } = false;

    public override string ToString()
    {
        return $"TopicEnqueue: [Topic={Topic}] [QualityOfServiceLevel={QualityOfServiceLevel}] [Retain={Retain}]";
    }
}