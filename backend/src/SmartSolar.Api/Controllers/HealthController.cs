using Microsoft.AspNetCore.Mvc;
using MongoDB.Bson;
using MongoDB.Driver;

namespace SmartSolar.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class HealthController : ControllerBase
{
    private readonly IMongoDatabase _database;

    public HealthController(IMongoDatabase database)
    {
        _database = database;
    }

    // Return a payload proving the API process is up and can reach MongoDB.
    [HttpGet]
    public async Task<IActionResult> Get(CancellationToken cancellationToken)
    {
        try
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(TimeSpan.FromSeconds(3));

            await _database.RunCommandAsync((Command<BsonDocument>)"{ping:1}", cancellationToken: cts.Token);

            return Ok(new
            {
                status = "ok",
                service = "SmartSolar.Api",
                database = "connected",
                utc = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            return StatusCode(503, new
            {
                status = "degraded",
                service = "SmartSolar.Api",
                database = "disconnected",
                error = ex.Message,
                utc = DateTime.UtcNow
            });
        }
    }
}
