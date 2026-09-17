using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartSolar.Api.DTOs.Request.Nodes;
using SmartSolar.Api.Services;

namespace SmartSolar.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class NodesController : ControllerBase
{
    private readonly NodeService _nodeService;

    // Inject the service. The controller holds no state of its own.
    public NodesController(NodeService nodeService)
    {
        _nodeService = nodeService;
    }

    /// <summary>
    /// GET /api/nodes?active=&amp;page=&amp;pageSize=
    /// Any authenticated caller. Map the service tuple into the paged envelope.
    /// </summary>
    [HttpGet]
    public Task<IActionResult> GetAll(
        [FromQuery] bool? active, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
        => throw new NotImplementedException();

    /// <summary>
    /// GET /api/nodes/{nodeId}
    /// Any authenticated caller. E4404 from the service becomes a 404 envelope.
    /// </summary>
    [HttpGet("{nodeId}")]
    public Task<IActionResult> GetById(string nodeId)
        => throw new NotImplementedException();

    /// <summary>
    /// POST /api/nodes
    /// Backoffice only. The caller's UserId comes from the validated token -
    /// User.FindFirstValue(ClaimTypes.NameIdentifier) - never from the body.
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "Backoffice")]
    public Task<IActionResult> Create([FromBody] CreateNodeRequest request)
        => throw new NotImplementedException();

    /// <summary>
    /// PUT /api/nodes/{nodeId}
    /// Backoffice only.
    /// </summary>
    [HttpPut("{nodeId}")]
    [Authorize(Roles = "Backoffice")]
    public Task<IActionResult> Update(string nodeId, [FromBody] UpdateNodeRequest request)
        => throw new NotImplementedException();

    // There is deliberately no DELETE. REQ-NODE EC-4: nodes are deactivated,
    // never hard deleted, because their historical reservations stay readable.
}
