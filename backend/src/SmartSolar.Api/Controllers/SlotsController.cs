// ---------------------------------------------------------------------------
//  File        : SlotsController.cs
//  Project     : Smart Solar Microgrid Trading System - Web API
//  Description : HTTP surface for energy booking slot CRUD. Binds, authorises
//                and maps only - every business rule lives in
//                EnergyBookingSlotService. Routes are absolute (api-contract.md
//                §5 splits the resource across /api/nodes/{nodeId}/slots and
//                /api/slots/{slotId}), so no [controller] token is used.
// ---------------------------------------------------------------------------

using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartSolar.Api.DTOs.Request.Slots;
using SmartSolar.Api.Services;

namespace SmartSolar.Api.Controllers;

[ApiController]
[Authorize]
public class SlotsController : ControllerBase
{
    private readonly EnergyBookingSlotService _slotService;

    // Inject the service. The controller holds no state of its own.
    public SlotsController(EnergyBookingSlotService slotService)
    {
        _slotService = slotService;
    }

    /// <summary>
    /// GET /api/nodes/{nodeId}/slots?from=&amp;to=
    /// Any authenticated caller. A Prosumer only ever sees future, bookable slots (AC-9).
    /// </summary>
    [HttpGet("/api/nodes/{nodeId}/slots")]
    public async Task<IActionResult> GetByNode(string nodeId, [FromQuery] DateTime? from, [FromQuery] DateTime? to)
    {
        var callerRole = User.FindFirstValue(ClaimTypes.Role) ?? string.Empty;
        var slots = await _slotService.GetByNodeIdAsync(nodeId, from, to, callerRole);
        return Ok(slots);
    }

    /// <summary>
    /// POST /api/nodes/{nodeId}/slots
    /// Backoffice or Operator (own nodes only - EC-4).
    /// </summary>
    [HttpPost("/api/nodes/{nodeId}/slots")]
    [Authorize(Roles = "Backoffice,Operator")]
    public async Task<IActionResult> Create(string nodeId, [FromBody] CreateSlotRequest request)
    {
        var (callerUserId, callerRole) = CurrentCaller();
        var response = await _slotService.CreateAsync(nodeId, request, callerUserId, callerRole);
        return CreatedAtAction(nameof(GetByNode), new { nodeId }, response);
    }

    /// <summary>
    /// PUT /api/slots/{slotId}
    /// Backoffice or Operator (own nodes only - EC-4). Re-validated like a create (REQ-SLOT 4.3).
    /// </summary>
    [HttpPut("/api/slots/{slotId}")]
    [Authorize(Roles = "Backoffice,Operator")]
    public async Task<IActionResult> Update(string slotId, [FromBody] UpdateSlotRequest request)
    {
        var (callerUserId, callerRole) = CurrentCaller();
        var response = await _slotService.UpdateAsync(slotId, request, callerUserId, callerRole);
        return Ok(response);
    }

    /// <summary>
    /// PATCH /api/slots/{slotId}/availability
    /// Backoffice or Operator (own nodes only) - the day-to-day availability update.
    /// </summary>
    [HttpPatch("/api/slots/{slotId}/availability")]
    [Authorize(Roles = "Backoffice,Operator")]
    public async Task<IActionResult> UpdateAvailability(string slotId, [FromBody] UpdateSlotAvailabilityRequest request)
    {
        var (callerUserId, callerRole) = CurrentCaller();
        var response = await _slotService.UpdateAvailabilityAsync(slotId, request, callerUserId, callerRole);
        return Ok(response);
    }

    /// <summary>
    /// DELETE /api/slots/{slotId}
    /// Backoffice only.
    /// </summary>
    [HttpDelete("/api/slots/{slotId}")]
    [Authorize(Roles = "Backoffice")]
    public async Task<IActionResult> Delete(string slotId)
    {
        await _slotService.DeleteAsync(slotId);
        return Ok();
    }

    // Read the caller's identity and role from the validated token, never from the body.
    private (string UserId, string Role) CurrentCaller()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub") ?? string.Empty;
        var role = User.FindFirstValue(ClaimTypes.Role) ?? string.Empty;
        return (userId, role);
    }
}
