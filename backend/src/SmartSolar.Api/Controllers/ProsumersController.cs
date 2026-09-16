// ---------------------------------------------------------------------------
//  File        : ProsumersController.cs
//  Project     : Smart Solar Microgrid Trading System - Web API
//  Module      : Controllers
//  Description : Endpoints for prosumer management. Includes the public
//                register endpoint, and BR-03 guarded reactivation.
//  Author      : <member name - M1>
//  Created     : 2026-09-16
// ---------------------------------------------------------------------------

using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartSolar.Api.DTOs.Prosumers;
using SmartSolar.Api.Services;

namespace SmartSolar.Api.Controllers;

[ApiController]
[Route("api/prosumers")]
public class ProsumersController : ControllerBase
{
    private readonly ProsumerService _prosumerService;

    public ProsumersController(ProsumerService prosumerService)
    {
        _prosumerService = prosumerService;
    }

    /// <summary>
    /// Registers a new prosumer profile.
    /// </summary>
    [HttpPost("register")]
    [AllowAnonymous]
    public async Task<IActionResult> Register([FromBody] RegisterProsumerRequest request)
    {
        var response = await _prosumerService.RegisterAsync(request);
        return CreatedAtAction(nameof(GetByNic), new { nic = response.Nic }, response);
    }

    /// <summary>
    /// Gets a paginated list of all prosumers.
    /// </summary>
    [HttpGet]
    [Authorize(Roles = "Backoffice,Operator")]
    public async Task<IActionResult> GetAll(
        [FromQuery] string? status, [FromQuery] string? search,
        [FromQuery] int page = 1, [FromQuery] int pageSize = 10)
    {
        var response = await _prosumerService.GetAllAsync(status, search, page, pageSize);
        return Ok(response);
    }

    /// <summary>
    /// Gets a paginated list of prosumers waiting for activation.
    /// </summary>
    [HttpGet("pending")]
    [Authorize(Roles = "Backoffice")] // Only Backoffice can process the queue
    public async Task<IActionResult> GetPending([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
    {
        var response = await _prosumerService.GetPendingAsync(page, pageSize);
        return Ok(response);
    }

    /// <summary>
    /// Gets a specific prosumer by NIC.
    /// </summary>
    [HttpGet("{nic}")]
    [Authorize] // Handled by service BR-02 check
    public async Task<IActionResult> GetByNic(string nic)
    {
        var callerNic = User.FindFirstValue(ClaimTypes.NameIdentifier)
                     ?? User.FindFirstValue("sub") ?? string.Empty;
        var callerRole = User.FindFirstValue(ClaimTypes.Role) ?? string.Empty;

        var response = await _prosumerService.GetByNicAsync(nic, callerNic, callerRole);
        return Ok(response);
    }

    /// <summary>
    /// Updates a specific prosumer's profile details.
    /// </summary>
    [HttpPut("{nic}")]
    [Authorize] // Handled by service BR-02 check
    public async Task<IActionResult> Update(string nic, [FromBody] UpdateProsumerRequest request)
    {
        var callerNic = User.FindFirstValue(ClaimTypes.NameIdentifier)
                     ?? User.FindFirstValue("sub") ?? string.Empty;
        var callerRole = User.FindFirstValue(ClaimTypes.Role) ?? string.Empty;

        var response = await _prosumerService.UpdateAsync(nic, request, callerNic, callerRole);
        return Ok(response);
    }

    /// <summary>
    /// Approves or reactivates a prosumer profile.
    /// </summary>
    [HttpPatch("{nic}/activate")]
    [Authorize(Roles = "Backoffice")] // BR-03 enforcement at the router layer
    public async Task<IActionResult> Activate(string nic)
    {
        var callerRole = User.FindFirstValue(ClaimTypes.Role) ?? string.Empty;
        var callerUserId = User.FindFirstValue(ClaimTypes.NameIdentifier)
                        ?? User.FindFirstValue("sub") ?? string.Empty;

        await _prosumerService.ActivateAsync(nic, callerRole, callerUserId);
        return Ok();
    }

    /// <summary>
    /// Deactivates a prosumer profile.
    /// </summary>
    [HttpPatch("{nic}/deactivate")]
    [Authorize]
    public async Task<IActionResult> Deactivate(string nic)
    {
        var callerNic = User.FindFirstValue(ClaimTypes.NameIdentifier)
                     ?? User.FindFirstValue("sub") ?? string.Empty;
        var callerRole = User.FindFirstValue(ClaimTypes.Role) ?? string.Empty;

        await _prosumerService.DeactivateAsync(nic, callerNic, callerRole);
        return Ok();
    }
}
