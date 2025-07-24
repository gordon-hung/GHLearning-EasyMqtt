using System.Collections.Concurrent;

namespace GHLearning.EasyMqtt;

internal class MQTTTopicReceivedManage : IMqttTopicReceivedManage
{
    private readonly ConcurrentDictionary<string, MqttTopicReceivedFilter> _mqttTopicReceivedFilters;
    private readonly ConcurrentDictionary<string, IMqttTopicReceivedHandle> _mqttTopicReceivedHandles;

    public MQTTTopicReceivedManage()
    {
        _mqttTopicReceivedFilters = new ConcurrentDictionary<string, MqttTopicReceivedFilter>(StringComparer.OrdinalIgnoreCase);
        _mqttTopicReceivedHandles = new ConcurrentDictionary<string, IMqttTopicReceivedHandle>(StringComparer.OrdinalIgnoreCase);
    }

    public bool IsMqttTopicReceivedFilterEmpty => _mqttTopicReceivedFilters.IsEmpty;

    public bool IsMqttTopicReceivedHandleEmpty => _mqttTopicReceivedHandles.IsEmpty;

    public IAsyncEnumerable<MqttTopicReceivedFilter> GetMqttTopicReceivedFilters()
        => _mqttTopicReceivedFilters.Values.ToAsyncEnumerable();

    public bool TryAdd(MqttTopicReceivedFilter topicReceivedFilter, IMqttTopicReceivedHandle topicReceivedHandle)
    {
        if (!_mqttTopicReceivedFilters.TryAdd(topicReceivedFilter.Topic, topicReceivedFilter))
        {
            throw new Exception($"Topic {topicReceivedFilter.Topic} already exists.");
        }
        if (!_mqttTopicReceivedHandles.TryAdd(topicReceivedFilter.Topic, topicReceivedHandle))
        {
            throw new Exception($"Received handle for topic {topicReceivedFilter.Topic} already exists.");
        }

        return true;
    }

    public bool TryGetValue(string topic, out IMqttTopicReceivedHandle topicReceivedHandle)
        => _mqttTopicReceivedHandles.TryGetValue(topic, out topicReceivedHandle!);

    public bool TryGetValue(string topic, out MqttTopicReceivedFilter topicFilter)
        => _mqttTopicReceivedFilters.TryGetValue(topic, out topicFilter!);

    public bool TryRemove(string topic)
    {
        return _mqttTopicReceivedFilters.TryRemove(topic, out _) && _mqttTopicReceivedHandles.TryRemove(topic, out _);
    }
}