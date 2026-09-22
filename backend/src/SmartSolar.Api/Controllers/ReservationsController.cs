// ---------------------------------------------------------------------------
//  File        : ReservationsController.cs
//  Project     : Smart Solar Microgrid Trading System - Web API
//  Description : REST controller managing the reservation lifecycle, approvals,
//                cancellations, modifications, and QR codes (DOM-03).
// ---------------------------------------------------------------------------

using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartSolar.Api.DTOs.Request.Reservations;
using SmartSolar.Api.DTOs.Response.Common;
using SmartSolar.Api.DTOs.Response.Reservations;
using SmartSolar.Api.Services;

namespace SmartSolar.Api.Controllers;

[ApiController]
[Route("api/reservations")]
public class ReservationsController : ControllerBase
{
    private readonly ReservationService _reservationService;

    // Injects the reservation business service.
    public ReservationsController(ReservationService reservationService)
    {
        // Inject reservation business service.
        _reservationService = reservationService;
    }

    /// <summary>
    /// Creates a new energy transfer reservation for the authenticated prosumer.
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "Prosumer")]
    public async Task<IActionResult> Create([FromBody] CreateReservationRequest request)
    {
        // Extract caller's authenticated NIC from the JWT token claims.
        var callerNic = GetCallerSubject();
        var response = await _reservationService.CreateAsync(callerNic, request);
        return CreatedAtAction(nameof(GetById), new { reservationId = response.ReservationId }, response);
    }

    /// <summary>
    /// Gets a paginated list of reservations matching staff filter criteria.
    /// </summary>
    [HttpGet]
    [Authorize(Roles = "Backoffice,Operator")]
    public async Task<IActionResult> GetAll(
        [FromQuery] string? status,
        [FromQuery] string? nodeId,
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        [FromQuery] string? search,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10)
    {
        // Query reservation records with operational staff filters.
        var response = await _reservationService.GetAllAsync(status, nodeId, from, to, search, page, pageSize);
        return Ok(response);
    }

    /// <summary>
    /// Gets a paginated list of reservations belonging to the authenticated prosumer.
    /// </summary>
    [HttpGet("me")]
    [Authorize(Roles = "Prosumer")]
    public async Task<IActionResult> GetMine(
        [FromQuery] string? status,
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        [FromQuery] string? search,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10)
    {
        // Query bookings belonging to the calling prosumer.
        var callerNic = GetCallerSubject();
        var response = await _reservationService.GetMineAsync(callerNic, status, from, to, search, page, pageSize);
        return Ok(response);
    }

    /// <summary>
    /// Gets a specific reservation by its unique reservation identifier.
    /// </summary>
    [HttpGet("{reservationId}")]
    [Authorize(Roles = "Prosumer,Operator,Backoffice")]
    public async Task<IActionResult> GetById(string reservationId)
    {
        // Load reservation details and enforce caller ownership permissions.
        var callerSubject = GetCallerSubject();
        var callerRole = GetCallerRole();
        var response = await _reservationService.GetByIdAsync(reservationId, callerSubject, callerRole);
        return Ok(response);
    }

    /// <summary>
    /// Modifies an existing reservation slot or kWh amount within the allowed notice window.
    /// </summary>
    [HttpPut("{reservationId}")]
    [Authorize(Roles = "Prosumer,Operator,Backoffice")]
    public async Task<IActionResult> Update(string reservationId, [FromBody] UpdateReservationRequest request)
    {
        // Modify reservation schedule and enforce 12-hour notice rule (BR-06).
        var callerSubject = GetCallerSubject();
        var callerRole = GetCallerRole();
        var response = await _reservationService.UpdateAsync(reservationId, request, callerSubject, callerRole);
        return Ok(response);
    }

    /// <summary>
    /// Approves a pending reservation, decrements slot capacity, and generates a QR code.
    /// </summary>
    [HttpPatch("{reservationId}/approve")]
    [Authorize(Roles = "Backoffice,Operator")]
    public async Task<IActionResult> Approve(string reservationId)
    {
        // Approve reservation, account for slot capacity, and issue signed QR token.
        var response = await _reservationService.ApproveAsync(reservationId);
        return Ok(response);
    }

    /// <summary>
    /// Cancels a reservation with at least 12 hours' notice and restores slot capacity.
    /// </summary>
    [HttpPatch("{reservationId}/cancel")]
    [Authorize(Roles = "Prosumer,Operator,Backoffice")]
    public async Task<IActionResult> Cancel(string reservationId)
    {
        // Cancel reservation and enforce 12-hour cancellation notice rule (BR-07).
        var callerSubject = GetCallerSubject();
        var callerRole = GetCallerRole();
        var response = await _reservationService.CancelAsync(reservationId, callerSubject, callerRole);
        return Ok(response);
    }

    /// <summary>
    /// Retrieves the signed QR code token for an approved reservation.
    /// </summary>
    [HttpGet("{reservationId}/qr")]
    [Authorize(Roles = "Prosumer")]
    public async Task<IActionResult> GetQr(string reservationId)
    {
        // Fetch single-use signed QR token for prosumer scanning and verification.
        var callerNic = GetCallerSubject();
        var callerRole = GetCallerRole();
        var response = await _reservationService.GetQrAsync(reservationId, callerNic, callerRole);
        return Ok(response);
    }

    // Helper extracting the primary identity subject claim from current principal.
    private string GetCallerSubject()
    {
        // Extract identity subject claim from caller claims principal.
        return User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.FindFirstValue("sub")
            ?? string.Empty;
    }

    // Helper extracting the role claim from current principal.
    private string GetCallerRole()
    {
        // Extract role claim from caller claims principal.
        return User.FindFirstValue(ClaimTypes.Role) ?? string.Empty;
    }
}
