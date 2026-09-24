<<<<<<< HEAD
namespace SmartSolar.Api.DTOs.Response.Dashboard;

/// <summary>
/// GET /dashboard/backoffice 200 response (api-contract.md §8).
/// </summary>
=======
// ---------------------------------------------------------------------------
//  File        : BackofficeDashboardResponse.cs
//  Project     : Smart Solar Microgrid Trading System - Web API
//  Description : Response DTO representing aggregated system metrics for the
//                backoffice administration dashboard (REQ-VIEW §2.2).
// ---------------------------------------------------------------------------

namespace SmartSolar.Api.DTOs.Response.Dashboard;

>>>>>>> develop
public record BackofficeDashboardResponse(
    int TotalProsumers,
    int PendingActivations,
    int ActiveNodes,
    int TotalReservations,
    Dictionary<string, int> ReservationsByStatus
);
