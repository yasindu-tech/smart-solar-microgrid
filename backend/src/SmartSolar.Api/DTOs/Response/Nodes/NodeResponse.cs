using SmartSolar.Api.DTOs.Common.Nodes;

namespace SmartSolar.Api.DTOs.Response.Nodes;

/// <summary>What every node endpoint returns.</summary>
public record NodeResponse(
    string NodeId,
    string Name,
    NodeLocationDto Location,
    double CapacityKwh,
    int BatterySlotCount,
    string Type,
    List<ScheduleWindowDto> Schedule,
    bool IsActive,
    string CreatedBy,
    DateTime CreatedAt,
    DateTime UpdatedAt);
