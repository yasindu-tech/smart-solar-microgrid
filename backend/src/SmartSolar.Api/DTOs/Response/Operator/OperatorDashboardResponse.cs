// ---------------------------------------------------------------------------
//  File        : OperatorDashboardResponse.cs
//  Project     : Smart Solar Microgrid Trading System - Web API
//  Description : Response DTO carrying operator dashboard counts and today's
//                schedule, scoped to the operator's assigned nodes (REQ-VIEW §2.3).
// ---------------------------------------------------------------------------

using SmartSolar.Api.DTOs.Response.Reservations;

namespace SmartSolar.Api.DTOs.Response.Operator;

public record OperatorDashboardResponse(
    int PendingCount,
    int ApprovedFutureCount,
    List<ReservationResponse> TodaysReservations,
    List<string> ScopedNodeIds
);
