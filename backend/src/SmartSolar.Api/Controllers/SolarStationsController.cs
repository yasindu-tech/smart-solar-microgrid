// ---------------------------------------------------------------------------
//  File        : SolarStationsController.cs
//  Project     : Smart Solar Microgrid Trading System - Web API
//  Description : HTTP surface for microgrid node CRUD. Binds, authorises and
//                maps only - every business rule lives in SolarStationService.
//                Route stays "api/nodes" (frozen in api-contract.md) even
//                though the class is named for the SolarStationInfo
//                collection it manages.
// ---------------------------------------------------------------------------

using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartSolar.Api.DTOs.Request.Nodes;
using SmartSolar.Api.DTOs.Response.Common;
using SmartSolar.Api.DTOs.Response.Nodes;
using SmartSolar.Api.Services;

namespace SmartSolar.Api.Controllers;

[ApiController]
[Route("api/nodes")]
[Authorize]
public class SolarStationsController : ControllerBase
{
    private readonly SolarStationService _nodeService;

    // Inject the service. The controller holds no state of its own.
    public SolarStationsController(SolarStationService nodeService)
    {
        _nodeService = nodeService;
    }

    /// <summary>
    /// GET /api/nodes?active=&amp;page=&amp;pageSize=
    /// Any authenticated caller. Map the service tuple into the paged envelope.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] bool? active, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        // Delegate paging/filtering to the service; it owns the clamp rules.
        var (items, total, effectivePage, effectivePageSize) =
            await _nodeService.GetAllAsync(active, page, pageSize);

        return Ok(new PagedResponse<NodeResponse>(items, total, effectivePage, effectivePageSize));
    }

    /// <summary>
    /// GET /api/nodes/{nodeId}
    /// Any authenticated caller. E4404 from the service becomes a 404 envelope.
    /// </summary>
    [HttpGet("{nodeId}")]
    public async Task<IActionResult> GetById(string nodeId)
    {
        // The service throws E4404 for an unknown id; the error middleware maps it to 404.
        var response = await _nodeService.GetByIdAsync(nodeId);
        return Ok(response);
    }

    /// <summary>
    /// POST /api/nodes
    /// Backoffice only. The caller's UserId comes from the validated token -
    /// User.FindFirstValue(ClaimTypes.NameIdentifier) - never from the body.
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "Backoffice")]
    public async Task<IActionResult> Create([FromBody] CreateNodeRequest request)
    {
        // Read the caller's identity from the token, never trust a body field for it.
        var callerUserId = User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.FindFirstValue("sub") ?? string.Empty;

        var response = await _nodeService.CreateAsync(request, callerUserId);
        return CreatedAtAction(nameof(GetById), new { nodeId = response.NodeId }, response);
    }

    /// <summary>
    /// PUT /api/nodes/{nodeId}
    /// Backoffice only.
    /// </summary>
    [HttpPut("{nodeId}")]
    [Authorize(Roles = "Backoffice")]
    public async Task<IActionResult> Update(string nodeId, [FromBody] UpdateNodeRequest request)
    {
        // The service re-runs every create-time validation and rejects an unknown id with E4404.
        var response = await _nodeService.UpdateAsync(nodeId, request);
        return Ok(response);
    }

    // There is deliberately no DELETE. REQ-NODE EC-4: nodes are deactivated,
    // never hard deleted, because their historical reservations stay readable.
}
