// ---------------------------------------------------------------------------
//  File        : HealthController.cs
//  Project     : Smart Solar Microgrid Trading System - Web API
//  Description : Health and readiness probe verifying API status and MongoDB connectivity.
// ---------------------------------------------------------------------------

using Microsoft.AspNetCore.Mvc;
using MongoDB.Bson;
using MongoDB.Driver;

namespace SmartSolar.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class HealthController : ControllerBase
{
    private readonly IMongoDatabase _database;

    // Injects the shared database used by the readiness probe.
    public HealthController(IMongoDatabase database)
    {
        _database = database;
    }

    // Pings MongoDB to verify readiness and returns service health status.
    [HttpGet]
    public async Task<IActionResult> Get(CancellationToken cancellationToken)
    {
        // Execute readiness probe with a 3-second cancellation timeout.
        try
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(TimeSpan.FromSeconds(3));

            await _database.RunCommandAsync((Command<BsonDocument>)"{ping:1}", cancellationToken: cts.Token);

            // Return healthy status when database ping succeeds.
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
            // Report degraded readiness status when database connection fails.
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
