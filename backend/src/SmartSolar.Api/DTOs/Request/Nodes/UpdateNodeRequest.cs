// ---------------------------------------------------------------------------
//  UpdateNodeRequest.cs - PUT /api/nodes/{nodeId} request body.
//  REQ-NODE - Microgrid Node Management
// ---------------------------------------------------------------------------

using SmartSolar.Api.DTOs.Common.Nodes;

namespace SmartSolar.Api.DTOs.Request.Nodes;

/// <summary>PUT /api/nodes/{nodeId} body. nodeId and isActive are not editable here.</summary>
public record UpdateNodeRequest(
    string Name,
    NodeLocationDto Location,
    double CapacityKwh,
    int BatterySlotCount,
    string Type,
    List<ScheduleWindowDto> Schedule);
