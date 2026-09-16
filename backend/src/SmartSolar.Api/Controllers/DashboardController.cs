// ---------------------------------------------------------------------------
//  File        : DashboardController.cs
//  Project     : Smart Solar Microgrid Trading System - Web API
//  Module      : Controllers
//  Description : Shared controller for the 3 dashboards.
//                D1 owns /api/dashboard/backoffice.
//                D3 owns /api/dashboard/prosumer.
//                D4 owns /api/dashboard/operator.
//  Author      : <member name - M1>
//  Created     : 2026-09-16
// ---------------------------------------------------------------------------

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartSolar.Api.Services;

namespace SmartSolar.Api.Controllers;

[ApiController]
[Route("api/dashboard")]
public class DashboardController : ControllerBase
{
    private readonly ProsumerService _prosumerService;
    // TODO (M3): Inject ReservationService
    // TODO (M4): Inject OperatorService

    public DashboardController(ProsumerService prosumerService)
    {
        _prosumerService = prosumerService;
    }

    // REQ-VIEW §2.4: Backoffice dashboard (Owned by D1)
    [HttpGet("backoffice")]
    [Authorize(Roles = "Backoffice")]
    public async Task<IActionResult> GetBackofficeDashboard()
    {
        var response = await _prosumerService.GetBackofficeDashboardAsync();
        return Ok(response);
    }

    // REQ-VIEW §2.1: Prosumer dashboard (Owned by D3)
    [HttpGet("prosumer")]
    [Authorize(Roles = "Prosumer")]
    public IActionResult GetProsumerDashboard()
    {
        return StatusCode(501, new { message = "Not implemented yet. Over to M3." });
    }

    // REQ-VIEW §2.3: Operator dashboard (Owned by D4)
    [HttpGet("operator")]
    [Authorize(Roles = "Operator")]
    public IActionResult GetOperatorDashboard()
    {
        return StatusCode(501, new { message = "Not implemented yet. Over to M4." });
    }
}
