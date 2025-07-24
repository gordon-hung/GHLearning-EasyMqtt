using MQTTnet.Client;
using System.Diagnostics;
using System.Text;
using System.Text.Json.Nodes;

namespace GHLearning.EasyMqtt;

internal class MqttApplicationMessageTraceReceivedHandler(
   IMqttApplicationMessageReceivedHandler mqttApplicationMessageReceivedHandler,
   ActivitySource activitySource) : IMqttApplicationMessageReceivedHandler
{
    public Task HandleMessageAsync(MqttApplicationMessageReceivedEventArgs arg)
    {
        var topic = arg?.ApplicationMessage?.Topic;
        var payloadText = arg?.ApplicationMessage?.PayloadSegment ?? Array.Empty<byte>();

        var jsonNode = JsonNode.Parse(Encoding.UTF8.GetString(payloadText));
        var spanId = jsonNode?["SpanId"]?.GetValue<string>() ?? string.Empty;
        var traceId = jsonNode?["TraceId"]?.GetValue<string>() ?? string.Empty;

        using var activity = (!string.IsNullOrEmpty(traceId) && !string.IsNullOrEmpty(spanId)) ?
            activitySource.CreateActivity("Mqtt Application Message Received Handler", ActivityKind.Internal)?
            .SetParentId(
                traceId: ActivityTraceId.CreateFromString(traceId),
                spanId: ActivitySpanId.CreateFromString(spanId),
                activityTraceFlags: ActivityTraceFlags.Recorded)
            : activitySource.CreateActivity("Mqtt Application Message Received Handler", ActivityKind.Internal);
        try
        {
            activity?.SetTag("mqtt.application.message.received.namespace", typeof(IMqttApplicationMessageReceivedHandler).Namespace);
            activity?.SetTag("mqtt.application.message.received.name", typeof(IMqttApplicationMessageReceivedHandler).Name);
            activity?.SetTag("mqtt.application.message.received.topic", topic);

            activity?.Start();

            return mqttApplicationMessageReceivedHandler.HandleMessageAsync(arg);
        }
        finally
        {
            activity?.Stop();
        }
    }
}