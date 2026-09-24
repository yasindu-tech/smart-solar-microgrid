// ---------------------------------------------------------------------------
//  File        : DashboardController.cs
//  Project     : Smart Solar Microgrid Trading System - Web API
//  Description : Thin controller for the dashboard aggregate endpoints
//                (api-contract.md §8, REQ-VIEW §2.1, DOM-03). Each dashboard
//                is scoped by role.
// ---------------------------------------------------------------------------

using System.Security.Claims;
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

    // Injects the dashboard aggregation service.
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

    /// <summary>
    /// Gets the home dashboard summary metrics and next upcoming reservation for the prosumer.
    /// </summary>
    [HttpGet("prosumer")]
    [Authorize(Roles = "Prosumer")]
    public async Task<IActionResult> GetProsumerDashboard()
    {
        // Compute dynamic metrics and upcoming bookings for the authenticated prosumer.
        var callerNic = User.FindFirstValue(ClaimTypes.NameIdentifier)
                     ?? User.FindFirstValue("sub")
                     ?? string.Empty;

        var response = await _dashboardService.GetProsumerDashboardAsync(callerNic);
        return Ok(response);
    }

    /// <summary>
    /// Gets aggregated system-wide metrics for the backoffice dashboard (REQ-VIEW §2.2).
    /// </summary>
    [HttpGet("backoffice")]
    [Authorize(Roles = "Backoffice,Operator")]
    public async Task<IActionResult> GetBackofficeDashboard()
    {
        var response = await _dashboardService.GetBackofficeDashboardAsync();
        return Ok(response);
    }
}

