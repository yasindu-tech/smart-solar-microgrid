// ---------------------------------------------------------------------------
//  File        : BackofficeDashboardResponse.cs
//  Project     : Smart Solar Microgrid Trading System - Web API
//  Module      : DTOs / Dashboard
//  Description : Response body for GET /api/dashboard/backoffice. All counts
//                come from the API — never hard-coded in the client
//                (REQ-VIEW §2.4, AC-13).
//  Author      : <member name - M1>
//  Created     : 2026-09-16
// ---------------------------------------------------------------------------

namespace SmartSolar.Api.DTOs.Dashboard;

public record BackofficeDashboardResponse(
    // Total registered prosumers across all statuses.
    int TotalProsumers,

    // Prosumers awaiting Backoffice approval.
    int PendingActivations,

    // Microgrid nodes that are currently active.
    int ActiveNodes,

    // Reservation counts keyed by status string (e.g. "Pending", "Approved").
    Dictionary<string, int> ReservationsByStatus
);
