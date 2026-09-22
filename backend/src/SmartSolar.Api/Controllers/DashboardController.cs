// ---------------------------------------------------------------------------
//  File        : DashboardController.cs
//  Project     : Smart Solar Microgrid Trading System - Web API
//  Description : Thin controller for the dashboard aggregate endpoints
//                (api-contract.md §8). Each dashboard is scoped by role.
// ---------------------------------------------------------------------------

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

    [HttpGet("backoffice")]
    [Authorize(Roles = "Backoffice")]
    public async Task<IActionResult> GetBackofficeDashboard()
    {
        var response = await _dashboardService.GetBackofficeDashboardAsync();
        return Ok(response);
    }
}
