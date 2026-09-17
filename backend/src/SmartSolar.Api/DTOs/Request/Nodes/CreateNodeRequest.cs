// ---------------------------------------------------------------------------
//  CreateNodeRequest.cs - POST /api/nodes request body.
//  REQ-NODE - Microgrid Node Management
// ---------------------------------------------------------------------------

using SmartSolar.Api.DTOs.Common.Nodes;

namespace SmartSolar.Api.DTOs.Request.Nodes;

/// <summary>POST /api/nodes body. nodeId is server-generated, never supplied.</summary>
public record CreateNodeRequest(
    string Name,
    NodeLocationDto Location,
    double CapacityKwh,
    int BatterySlotCount,
    string Type,
    List<ScheduleWindowDto> Schedule);
