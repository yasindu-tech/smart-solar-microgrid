namespace SmartSolar.Api.DTOs.Request.Slots;

/// <summary>POST /api/nodes/{nodeId}/slots body. slotId is server-generated, never supplied.</summary>
public record CreateSlotRequest(
    DateTime StartTime,
    DateTime EndTime,
    int TotalCapacity,
    double EnergyOfferedKwh);
