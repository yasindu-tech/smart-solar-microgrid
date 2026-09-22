// ---------------------------------------------------------------------------
//  File        : ReservationResponse.cs
//  Project     : Smart Solar Microgrid Trading System - Web API
//  Description : Response DTO representing an energy reservation contract.
// ---------------------------------------------------------------------------

namespace SmartSolar.Api.DTOs.Response.Reservations;

public record ReservationResponse(
    string ReservationId,
    string Nic,
    string NodeId,
    string SlotId,
    DateTime SlotStartTime,
    DateTime SlotEndTime,
    double RequestedKwh,
    string Status,
    string? QrToken,
    DateTime? QrIssuedAt,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    DateTime? CancelledAt,
    string? CancelledBy,
    DateTime? CompletedAt,
    string? CompletedByOperatorId,
    double? DeliveredKwh
);
