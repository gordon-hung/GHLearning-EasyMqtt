using GHLearning.EasyMqtt;
using GHLearning.EasyMqtt.DependencyInjection;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using MQTTnet.Client;
using MQTTnet.Extensions.ManagedClient;
using MQTTnet.Protocol;
using OpenTelemetry;
using OpenTelemetry.Exporter;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Prometheus;
using Scalar.AspNetCore;
using System.Net.Mime;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services
    .AddRouting(options => options.LowercaseUrls = true)
    .AddControllers(options =>
    {
        options.Filters.Add(new ProducesAttribute(MediaTypeNames.Application.Json));
        options.Filters.Add(new ConsumesAttribute(MediaTypeNames.Application.Json));
    })
    .AddJsonOptions(options => options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));

// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

// Register the system time provider for dependency injection
builder.Services.AddSingleton(TimeProvider.System);

// Learn more about configuring GHLearning.EasyMqtt
builder.Services.AddEasyMqttClient(
    managedMqttClientOptions: new ManagedMqttClientOptionsBuilder()
    .WithAutoReconnectDelay(TimeSpan.FromSeconds(60))
    .WithClientOptions(
        new MqttClientOptionsBuilder()
        .WithClientId(builder.Environment.ApplicationName)
        .WithTcpServer("localhost", 1883)
        .Build())
    .Build())
    .AddEasyMqttTopicEnqueueFilter(
    new MqttTopicEnqueueFilter
    {
        Topic = $"home/temperature/data",
        QualityOfServiceLevel = MqttQualityOfServiceLevel.AtLeastOnce,
        Retain = false
    })
    .AddEasyMqttTopicEnqueueFilter(
    new MqttTopicEnqueueFilter
    {
        Topic = $"home/humidity/data",
        QualityOfServiceLevel = MqttQualityOfServiceLevel.AtMostOnce,
        Retain = false
    });

//Learn more about configuring OpenTelemetry at https://learn.microsoft.com/zh-tw/dotnet/core/diagnostics/observability-with-otel
builder.Services.AddOpenTelemetry()
    .ConfigureResource(resource => resource
    .AddService(
        serviceName: builder.Configuration["ServiceName"]!.ToLower(),
        serviceNamespace: typeof(Program).Assembly.GetName().Name,
        serviceVersion: typeof(Program).Assembly.GetName().Version?.ToString() ?? "unknown"))
    .UseOtlpExporter(OtlpExportProtocol.Grpc, new Uri(builder.Configuration["OtelExporterOtlpEndpoint"]!))
    .WithMetrics(metrics => metrics
        .AddMeter("GHLearning.")
        .AddAspNetCoreInstrumentation()
        .AddRuntimeInstrumentation()
        .AddProcessInstrumentation()
        .AddPrometheusExporter())
    .WithTracing(tracing => tracing
        .AddSource("GHLearning.")
        .AddHttpClientInstrumentation()
        .AddAspNetCoreInstrumentation(options => options.Filter = (httpContext) =>
                !httpContext.Request.Path.StartsWithSegments("/swagger", StringComparison.OrdinalIgnoreCase) &&
                !httpContext.Request.Path.StartsWithSegments("/live", StringComparison.OrdinalIgnoreCase) &&
                !httpContext.Request.Path.StartsWithSegments("/healthz", StringComparison.OrdinalIgnoreCase) &&
                !httpContext.Request.Path.StartsWithSegments("/metrics", StringComparison.OrdinalIgnoreCase) &&
                !httpContext.Request.Path.StartsWithSegments("/favicon.ico", StringComparison.OrdinalIgnoreCase) &&
                !httpContext.Request.Path.Value!.Equals("/api/events/raw", StringComparison.OrdinalIgnoreCase) &&
                !httpContext.Request.Path.Value!.EndsWith(".js", StringComparison.OrdinalIgnoreCase) &&
                !httpContext.Request.Path.StartsWithSegments("/_vs", StringComparison.OrdinalIgnoreCase) &&
                !httpContext.Request.Path.StartsWithSegments("/openapi", StringComparison.OrdinalIgnoreCase) &&
                !httpContext.Request.Path.StartsWithSegments("/scalar", StringComparison.OrdinalIgnoreCase)));

//Learn more about configuring HealthChecks at https://learn.microsoft.com/zh-tw/aspnet/core/host-and-deploy/health-checks?view=aspnetcore-9.0
builder.Services
    .AddHealthChecks()
    .AddCheck("self", () => HealthCheckResult.Healthy(), ["live"]);

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwaggerUI(options => options.SwaggerEndpoint("/openapi/v1.json", "OpenAPI V1"));// swagger/
    app.UseReDoc(options => options.SpecUrl("/openapi/v1.json"));//api-docs/
    app.MapScalarApiReference();//scalar/v1
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.MapHealthChecks("/live", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("live"),
    ResultStatusCodes =
    {
        [HealthStatus.Healthy] = StatusCodes.Status200OK,
        [HealthStatus.Degraded] = StatusCodes.Status200OK,
        [HealthStatus.Unhealthy] = StatusCodes.Status503ServiceUnavailable
    }
});

app.MapHealthChecks("/healthz", new HealthCheckOptions
{
    Predicate = _ => true,
    ResultStatusCodes =
    {
        [HealthStatus.Healthy] = StatusCodes.Status200OK,
        [HealthStatus.Degraded] = StatusCodes.Status200OK,
        [HealthStatus.Unhealthy] = StatusCodes.Status503ServiceUnavailable
    }
});

// Prometheus 提供服務數據資料源
app.UseHttpMetrics();

// Enable GHLearning.EasyMqtt middleware to handle MQTT messages
app.UseEasyMqtt();

app.Run();