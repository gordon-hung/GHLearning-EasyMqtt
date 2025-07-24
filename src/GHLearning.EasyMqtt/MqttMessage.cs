using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Text.Json;

namespace GHLearning.EasyMqtt;

internal class MqttMessage(
    ILogger<MqttMessage> logger,
    IMqttClient client,
    ActivitySource activitySource) : IMqttMessage
{
    public Task EnqueueAsync(string topic, string payload, CancellationToken cancellationToken = default)
    {
        logger.LogDebug("Enqueueing message: Topic:{topic}, Payload:{payload}", topic, payload);
        using var activity = activitySource.StartActivity("Mqtt Message");
        // 設定相關的追蹤標籤 (Tags)，有助於後端追蹤系統分析
        activity?.SetTag("mqtt.request.namespace", typeof(IMqttMessage).Namespace);
        activity?.SetTag("mqtt.request.name", typeof(IMqttMessage).Name);
        activity?.SetTag("mqtt.topic", topic);  // 記錄當前的 MQTT 主題
        activity?.SetTag("mqtt.payload.length", payload.Length);  // 記錄 payload 的長度

        // 把追蹤上下文附加到 payload 中
        // 從當前 Activity 獲取追蹤上下文

        var enrichedPayload = new
        {
            TraceId = Activity.Current?.TraceId.ToString() ?? string.Empty,
            SpanId = Activity.Current?.SpanId.ToString() ?? string.Empty,
            Payload = payload  // 將原始 payload 包裝在一個對象中
        };

        // 記錄活動開始時間
        activity?.Start();

        // 註冊後可選地記錄更多的活動細節
        activity?.AddEvent(new ActivityEvent("Enqueue start", DateTime.UtcNow));

        // 呼叫 MQTT 客戶端的 EnqueueAsync 方法進行消息入列
        var even = client.EnqueueAsync(topic, JsonSerializer.Serialize(enrichedPayload));

        // 當任務完成時，進行後續處理
        even?.ContinueWith(t =>
        {
            if (t.IsCompletedSuccessfully)
            {
                // 記錄成功事件
                activity?.AddEvent(new ActivityEvent("Enqueue completed successfully"));
                activity?.SetStatus(ActivityStatusCode.Ok);
            }
            else
            {
                // 記錄錯誤事件
                activity?.AddEvent(new ActivityEvent("Enqueue failed"));
                activity?.SetStatus(ActivityStatusCode.Error);
                activity?.SetTag("error.message", t.Exception?.Message);
            }
        }, cancellationToken);

        // 返回原始的 EnqueueAsync 任務
        return even ?? Task.CompletedTask;
    }
}