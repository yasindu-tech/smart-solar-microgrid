// ---------------------------------------------------------------------------
//  File        : ProsumerDashboardResponse.cs
//  Project     : Smart Solar Microgrid Trading System - Web API
//  Description : Response DTO representing metrics and summary cards on the
//                prosumer mobile dashboard (REQ-VIEW §2.1).
// ---------------------------------------------------------------------------

using SmartSolar.Api.DTOs.Response.Reservations;

namespace SmartSolar.Api.DTOs.Response.Dashboard;

public record ProsumerDashboardResponse(
    int ActiveCount,
    int PendingCount,
    ReservationResponse? NextReservation,
    List<ReservationResponse> RecentHistory
);
