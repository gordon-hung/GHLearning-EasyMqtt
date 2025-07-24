using Microsoft.Extensions.Logging;
using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Extensions.ManagedClient;
using MQTTnet.Packets;

namespace GHLearning.EasyMqtt;

internal class MqttClient(
    ILogger<MqttClient> logger,
    ManagedMqttClientOptions options,
    IMqttTopicEnqueueManage mqttTopicEnqueueManage,
    IMqttTopicReceivedManage mqttTopicReceivedManage,
    IMqttApplicationMessageReceivedHandler mqttApplicationMessageReceivedHandler) : IMqttClient
{
    private readonly IManagedMqttClient _mqttClient = new MqttFactory().CreateManagedMqttClient();
    private readonly ManagedMqttClientOptions _managedMqttClientOptions = options ?? throw new ArgumentNullException(nameof(options));

    // 連接處理
    private Task OnConnectedAsync(MqttClientConnectedEventArgs arg)
    {
        logger.LogDebug("Connected");
        return Task.CompletedTask;
    }

    // 斷線處理
    private Task OnDisconnectedAsync(MqttClientDisconnectedEventArgs arg)
    {
        logger.LogWarning("Disconnected");
        return Task.CompletedTask;
    }

    // 連接失敗處理
    private Task OnConnectingFailedAsync(ConnectingFailedEventArgs arg)
    {
        logger.LogError("Connection failed, check network or broker!");
        return Task.CompletedTask;
    }

    // 連接狀態改變處理
    private Task OnConnectionStateChangedAsync(EventArgs arg)
    {
        logger.LogDebug("Connection state changed");
        return Task.CompletedTask;
    }

    // 訂閱同步失敗處理
    private Task OnSynchronizingSubscriptionsFailedAsync(ManagedProcessFailedEventArgs arg)
    {
        logger.LogError("Synchronizing subscriptions failed");
        return Task.CompletedTask;
    }

    // 訂閱改變處理
    private Task OnSubscriptionsChangedAsync(SubscriptionsChangedEventArgs arg)
    {
        logger.LogDebug("Subscriptions changed");
        return Task.CompletedTask;
    }

    public async Task StopAsync()
    {
        await _mqttClient.StopAsync();
    }

    public async Task StartAsync()
    {
        await _mqttClient.StartAsync(_managedMqttClientOptions);

        // 設定事件處理器
        _mqttClient.ConnectedAsync += OnConnectedAsync;
        _mqttClient.DisconnectedAsync += OnDisconnectedAsync;
        _mqttClient.ConnectingFailedAsync += OnConnectingFailedAsync;
        _mqttClient.ConnectionStateChangedAsync += OnConnectionStateChangedAsync;
        _mqttClient.SynchronizingSubscriptionsFailedAsync += OnSynchronizingSubscriptionsFailedAsync;
        _mqttClient.SubscriptionsChangedAsync += OnSubscriptionsChangedAsync;

        if (!mqttTopicReceivedManage.IsMqttTopicReceivedFilterEmpty)
        {
            var mqttTopicReceivedFilters = await mqttTopicReceivedManage.GetMqttTopicReceivedFilters().ToArrayAsync().ConfigureAwait(false);
            var mqttTopicFilters = mqttTopicReceivedFilters.Select(filter => new MqttTopicFilter
            {
                NoLocal = filter.NoLocal,
                QualityOfServiceLevel = filter.QualityOfServiceLevel,
                RetainAsPublished = filter.RetainAsPublished,
                RetainHandling = filter.RetainHandling,
                Topic = filter.Topic
            }).ToArray();
            await _mqttClient.SubscribeAsync(mqttTopicFilters);
        }
        _mqttClient.ApplicationMessageReceivedAsync += mqttApplicationMessageReceivedHandler.HandleMessageAsync;
    }

    public async Task EnqueueAsync(string topic, string payload)
    {
        if (!mqttTopicEnqueueManage.TryGetValue(topic, out var topicEnqueue))
        {
            throw new Exception($"Topic {topic} not found in enqueue filter.");
        }

        await _mqttClient.EnqueueAsync(
            topic: topicEnqueue.Topic,
            payload: payload,
            qualityOfServiceLevel: topicEnqueue.QualityOfServiceLevel,
            retain: topicEnqueue.Retain)
            .ConfigureAwait(false);
    }
}