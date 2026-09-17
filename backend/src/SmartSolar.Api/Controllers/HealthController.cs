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
