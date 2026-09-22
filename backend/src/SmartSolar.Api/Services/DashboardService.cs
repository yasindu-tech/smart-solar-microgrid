using SmartSolar.Api.DTOs.Response.Dashboard;
using SmartSolar.Api.Repositories;

namespace SmartSolar.Api.Services;

public class DashboardService
{
    private readonly ProsumerRepository _prosumerRepo;
    private readonly ISolarStationRepository _nodeRepo;

    // Inject repositories needed for the aggregation queries.
    public DashboardService(
        ProsumerRepository prosumerRepo,
        ISolarStationRepository nodeRepo)
    {
        _prosumerRepo = prosumerRepo;
        _nodeRepo = nodeRepo;
    }

    /// <summary>
    /// Builds the Backoffice dashboard payload (REQ-VIEW §2.4).
    /// Total prosumers, pending activations, active nodes, reservations by status.
    /// </summary>
    public async Task<BackofficeDashboardResponse> GetBackofficeDashboardAsync()
    {
        // Fire all count queries in parallel for faster response.
        var totalProsumersTask = _prosumerRepo.CountTotalAsync();
        var pendingActivationsTask = _prosumerRepo.CountByStatusAsync("PendingActivation");
        var activeNodesTask = _nodeRepo.CountActiveAsync();

        await Task.WhenAll(totalProsumersTask, pendingActivationsTask, activeNodesTask);

        // Reservation counts are stubbed until M3's ReservationRepository is available.
        // TODO (M3): Replace with real counts from the EnergyReservation collection.
        var reservationsByStatus = new ReservationsByStatusDto(
            Pending: 0,
            Approved: 0,
            Completed: 0,
            Cancelled: 0,
            Expired: 0
        );

        return new BackofficeDashboardResponse(
            TotalProsumers: totalProsumersTask.Result,
            PendingActivations: pendingActivationsTask.Result,
            ActiveNodes: activeNodesTask.Result,
            ReservationsByStatus: reservationsByStatus
        );
    }
}
