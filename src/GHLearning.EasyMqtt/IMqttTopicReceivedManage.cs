namespace GHLearning.EasyMqtt;

public interface IMqttTopicReceivedManage
{
    bool TryAdd(MqttTopicReceivedFilter topicReceivedFilter, IMqttTopicReceivedHandle topicReceivedHandle);

    bool TryRemove(string topic);

    bool TryGetValue(string topic, out IMqttTopicReceivedHandle topicReceivedHandle);

    bool TryGetValue(string topic, out MqttTopicReceivedFilter topicReceivedFilter);

    IAsyncEnumerable<MqttTopicReceivedFilter> GetMqttTopicReceivedFilters();

    bool IsMqttTopicReceivedFilterEmpty { get; }
    bool IsMqttTopicReceivedHandleEmpty { get; }
}