namespace SmartSolar.Api.DTOs.Response.Dashboard;

/// <summary>
/// GET /dashboard/backoffice 200 response (api-contract.md §8).
/// </summary>
public record BackofficeDashboardResponse(
    int TotalProsumers,
    int PendingActivations,
    int ActiveNodes,
    ReservationsByStatusDto ReservationsByStatus
);

/// <summary>
/// Reservation counts grouped by status for the backoffice dashboard.
/// </summary>
public record ReservationsByStatusDto(
    int Pending,
    int Approved,
    int Completed,
    int Cancelled,
    int Expired
);
