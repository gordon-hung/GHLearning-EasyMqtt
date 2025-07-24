using System.Collections.Concurrent;

namespace GHLearning.EasyMqtt;

internal class MqttTopicEnqueueManage : IMqttTopicEnqueueManage
{
    private readonly ConcurrentDictionary<string, MqttTopicEnqueueFilter> _mqttTopicEnqueueFilters;

    public MqttTopicEnqueueManage()
    {
        _mqttTopicEnqueueFilters = new ConcurrentDictionary<string, MqttTopicEnqueueFilter>(StringComparer.OrdinalIgnoreCase);
    }

    public bool TryAdd(MqttTopicEnqueueFilter topicEnqueueFilter)
        => _mqttTopicEnqueueFilters.TryAdd(topicEnqueueFilter.Topic, topicEnqueueFilter);

    public bool TryGetValue(string topic, out MqttTopicEnqueueFilter topicEnqueueFilter)
        => _mqttTopicEnqueueFilters.TryGetValue(topic, out topicEnqueueFilter!);

    public bool TryRemove(string topic)
        => _mqttTopicEnqueueFilters.TryRemove(topic, out _);
}