using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace GHLearning.EasyMqtt.Sensor.Controllers;

[Route("api/[controller]")]
[ApiController]
public class TemperatureController : ControllerBase
{
    [HttpPost]
    public Task EnqueueAsync(
        [FromServices] TimeProvider timeProvider,
        [FromServices] IMqttMessage message,
        [FromBody] double temperature)
        => message.EnqueueAsync(
            topic: $"home/temperature/data",
            payload: JsonSerializer.Serialize(new
            {
                SenAt = timeProvider.GetUtcNow(),
                Temperature = temperature
            }),
            cancellationToken: HttpContext.RequestAborted);
}