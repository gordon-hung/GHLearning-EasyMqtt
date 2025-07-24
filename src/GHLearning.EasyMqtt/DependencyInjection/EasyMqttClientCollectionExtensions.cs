using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using MQTTnet.Extensions.ManagedClient;

namespace GHLearning.EasyMqtt.DependencyInjection;

public static class EasyMqttClientCollectionExtensions
{
    public static IServiceCollection AddEasyMqttClient(
        this IServiceCollection services,
        ManagedMqttClientOptions managedMqttClientOptions)
    {
        _ = services
            .AddSingleton(TimeProvider.System)
            .AddSingleton<IMqttClient>((sp) => ActivatorUtilities.CreateInstance<MqttClient>(sp, managedMqttClientOptions))
            .AddSingleton<IMqttTopicReceivedManage>(new MQTTTopicReceivedManage())
            .AddSingleton<IMqttTopicEnqueueManage>(new MqttTopicEnqueueManage())
            .AddSingleton<IMqttApplicationMessageReceivedHandler, MqttApplicationMessageReceivedHandler>()
            .Decorate<IMqttApplicationMessageReceivedHandler>((innerRepository, sp) => ActivatorUtilities.CreateInstance<MqttApplicationMessageTraceReceivedHandler>(sp, innerRepository))
            .AddTransient<IMqttMessage, MqttMessage>();

        return services;
    }

    public static IServiceCollection AddEasyMqttTopicReceivedFilter<TReceivedHandle>(this IServiceCollection services,
        MqttTopicReceivedFilter topicFilter) where TReceivedHandle : class, IMqttTopicReceivedHandle
    {
        var concreteType = typeof(TReceivedHandle);
        var instance = Activator.CreateInstance(concreteType);
        if (instance is not IMqttTopicReceivedHandle receivedHandle)
        {
            throw new InvalidOperationException($"Failed to create an instance of {concreteType.FullName} that implements IReceivedHandle.");
        }
        var serviceProvider = services.BuildServiceProvider();
        var mqttClient = serviceProvider.GetRequiredService<IMqttTopicReceivedManage>();
        mqttClient.TryAdd(topicFilter, receivedHandle);
        return services;
    }

    public static IServiceCollection AddEasyMqttTopicEnqueueFilter(this IServiceCollection services,
        MqttTopicEnqueueFilter topicEnqueue)
    {
        var serviceProvider = services.BuildServiceProvider();
        var mqttClient = serviceProvider.GetRequiredService<IMqttTopicEnqueueManage>();
        mqttClient.TryAdd(topicEnqueue);
        return services;
    }

    public static IApplicationBuilder UseEasyMqtt(this IApplicationBuilder app)
    {
        var serviceProvider = app.ApplicationServices;
        var mqttClient = serviceProvider.GetRequiredService<IMqttClient>();
        mqttClient.StartAsync().GetAwaiter().GetResult();
        return app;
    }
}