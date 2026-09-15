// ---------------------------------------------------------------------------
//  File        : HealthController.cs
//  Project     : Smart Solar Microgrid Trading System - Web API
//  Module      : Controllers
//  Description : Liveness endpoint used by the CI smoke test and to confirm the
//                IIS deployment is reachable from both clients.
//  Author      : <member name - M1>
//  Created     : 2026-09-13
// ---------------------------------------------------------------------------

using Microsoft.AspNetCore.Mvc;

namespace SmartSolar.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class HealthController : ControllerBase
{
    // Return a small payload proving the API process is up and serving requests.
    [HttpGet]
    public IActionResult Get()
    {
        return Ok(new { status = "ok", service = "SmartSolar.Api", utc = DateTime.UtcNow });
    }
}
