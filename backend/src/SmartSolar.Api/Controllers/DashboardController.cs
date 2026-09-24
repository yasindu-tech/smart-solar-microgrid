using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartSolar.Api.Services;

namespace SmartSolar.Api.Controllers;

[ApiController]
[Route("api/dashboard")]
[Authorize]
public class DashboardController : ControllerBase
{
    private readonly DashboardService _dashboardService;

    // Inject the dashboard service.
    public DashboardController(DashboardService dashboardService)
    {
        _dashboardService = dashboardService;
    }

    /// <summary>
    /// Backoffice dashboard: totals, pending activations, active nodes,
    /// reservations grouped by status (REQ-VIEW §2.4).
    /// </summary>
    [HttpGet("backoffice")]
    [Authorize(Roles = "Backoffice")]
    public async Task<IActionResult> GetBackofficeDashboard()
    {
        var response = await _dashboardService.GetBackofficeDashboardAsync();
        return Ok(response);
    }
}
