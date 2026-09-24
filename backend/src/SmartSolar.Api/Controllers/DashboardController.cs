// ---------------------------------------------------------------------------
//  File        : DashboardController.cs
//  Project     : Smart Solar Microgrid Trading System - Web API
//  Description : Operational dashboard endpoints providing aggregated metrics.
//                Per-endpoint ownership (domains/README.md): the prosumer dashboard
//                belongs to DOM-03, the operator dashboard to DOM-04 (REQ-VIEW §2.3).
// ---------------------------------------------------------------------------

using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartSolar.Api.Services;

namespace SmartSolar.Api.Controllers;

[ApiController]
[Route("api/dashboard")]
public class DashboardController : ControllerBase
{
    private readonly DashboardService _dashboardService;

    // Injects the dashboard aggregation service.
    public DashboardController(DashboardService dashboardService)
    {
        // Inject the dashboard aggregation service.
        _dashboardService = dashboardService;
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
    /// Gets pending and approved-future counts plus today's schedule for the grid operator,
    /// scoped to that operator's assigned nodes (REQ-VIEW §2.3, BR-11).
    /// </summary>
    [HttpGet("operator")]
    [Authorize(Roles = "Operator")]
    public async Task<IActionResult> GetOperatorDashboard([FromQuery] string? nodeId)
    {
        // Compute node-scoped operational counts for the authenticated operator.
        var callerUserId = User.FindFirstValue(ClaimTypes.NameIdentifier)
                        ?? User.FindFirstValue("sub")
                        ?? string.Empty;

        var response = await _dashboardService.GetOperatorDashboardAsync(callerUserId, nodeId);
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

