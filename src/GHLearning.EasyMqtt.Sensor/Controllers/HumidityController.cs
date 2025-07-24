using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace GHLearning.EasyMqtt.Sensor.Controllers;

[Route("api/[controller]")]
[ApiController]
public class HumidityController : ControllerBase
{
    [HttpPost]
    public Task EnqueueAsync(
        [FromServices] TimeProvider timeProvider,
        [FromServices] IMqttMessage message,
        [FromBody] double humidity)
        => message.EnqueueAsync(
            topic: $"home/humidity/data",
            payload: JsonSerializer.Serialize(new
            {
                SenAt = timeProvider.GetUtcNow(),
                msg = humidity
            }),
            cancellationToken: HttpContext.RequestAborted);
}