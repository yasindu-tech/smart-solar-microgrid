namespace SmartSolar.Api.DTOs.Response.Slots;

/// <summary>What every slot endpoint returns.</summary>
public record SlotResponse(
    string SlotId,
    string NodeId,
    DateTime StartTime,
    DateTime EndTime,
    int TotalCapacity,
    int AvailableCapacity,
    double EnergyOfferedKwh,
    bool IsActive,
    DateTime CreatedAt,
    DateTime UpdatedAt);
