namespace GHLearning.EasyMqtt;

public interface IMqttTopicEnqueueManage
{
    bool TryAdd(MqttTopicEnqueueFilter topicEnqueueFilter);

    bool TryRemove(string topic);

    bool TryGetValue(string topic, out MqttTopicEnqueueFilter topicEnqueueFilter);
}