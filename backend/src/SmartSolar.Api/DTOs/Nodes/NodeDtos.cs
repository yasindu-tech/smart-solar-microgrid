namespace SmartSolar.Api.DTOs.Nodes;

/// <summary>One operating window, as the clients send and receive it.</summary>
public record ScheduleWindowDto(
    string DayOfWeek,
    string OpenTime,
    string CloseTime);

/// <summary>GPS position and address, as the clients send and receive it.</summary>
public record NodeLocationDto(
    double Latitude,
    double Longitude,
    string AddressLine);

/// <summary>POST /api/nodes body. nodeId is server-generated, never supplied.</summary>
public record CreateNodeRequest(
    string Name,
    NodeLocationDto Location,
    double CapacityKwh,
    int BatterySlotCount,
    string Type,
    List<ScheduleWindowDto> Schedule);

/// <summary>PUT /api/nodes/{nodeId} body. nodeId and isActive are not editable here.</summary>
public record UpdateNodeRequest(
    string Name,
    NodeLocationDto Location,
    double CapacityKwh,
    int BatterySlotCount,
    string Type,
    List<ScheduleWindowDto> Schedule);

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
