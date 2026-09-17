namespace SmartSolar.Api.DTOs.Common.Nodes;

/// <summary>One operating window, as the clients send and receive it.</summary>
public record ScheduleWindowDto(
    string DayOfWeek,
    string OpenTime,
    string CloseTime);
