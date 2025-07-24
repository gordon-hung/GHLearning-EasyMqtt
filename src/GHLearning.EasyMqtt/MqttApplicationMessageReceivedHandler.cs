using Microsoft.Extensions.Logging;
using MQTTnet.Client;
using System.Text;
using System.Text.Json.Nodes;

namespace GHLearning.EasyMqtt;

internal class MqttApplicationMessageReceivedHandler(
    ILogger<MqttApplicationMessageReceivedHandler> logger,
    IMqttTopicReceivedManage mqttTopicReceivedManage) : IMqttApplicationMessageReceivedHandler
{
    public Task HandleMessageAsync(MqttApplicationMessageReceivedEventArgs arg)
    {
        var topic = arg?.ApplicationMessage?.Topic;
        var payloadText = arg?.ApplicationMessage?.PayloadSegment ?? Array.Empty<byte>();

        logger.LogDebug("Received: Topic: {Topic}, Payload: {PayloadText}", topic, payloadText);

        // 如果 topic 為空則跳過處理
        if (string.IsNullOrWhiteSpace(topic))
        {
            logger.LogDebug("Received an empty or null topic. Skipping message processing.");
            return Task.CompletedTask;
        }

        // 處理 topic 路徑並轉換為 hash 路徑
        string[] pathParts = topic.Split('/');
        pathParts[^1] = "#";  // 最後一部分改為 '#'
        var topicHashPath = string.Join("/", pathParts);

        // 查找處理器
        if (!mqttTopicReceivedManage.TryGetValue(topic, out IMqttTopicReceivedHandle handle) && !mqttTopicReceivedManage.TryGetValue(topicHashPath, out handle))
        {
            logger.LogDebug("No handler found for topic: {Topic}", topic);
            return Task.CompletedTask;
        }

        var payloadTextString = Encoding.UTF8.GetString(payloadText);
        var payload = payloadTextString;
        try
        {
            var jsonNode = JsonNode.Parse(payloadTextString);
            payload = jsonNode?["Payload"]?.GetValue<string>() ?? payloadTextString;
        }
        catch
        {
            payload = payloadTextString;
        }

        return handle.ReceivedHandledAsync(Encoding.UTF8.GetBytes(payload));
    }
}