// ---------------------------------------------------------------------------
//  File        : DashboardService.cs
//  Project     : Smart Solar Microgrid Trading System - Web API
//  Description : Aggregates dynamic metrics and summary cards for the prosumer
//                mobile dashboard and backoffice dashboard (REQ-VIEW §2.1, §2.4, DOM-03).
// ---------------------------------------------------------------------------

using SmartSolar.Api.DTOs.Response.Dashboard;
using SmartSolar.Api.DTOs.Response.Reservations;
using SmartSolar.Api.Models;
using SmartSolar.Api.Repositories;

namespace SmartSolar.Api.Services;

public class DashboardService
{
    private readonly ProsumerRepository _prosumerRepo;
    private readonly ISolarStationRepository _nodeRepo;
    private readonly IReservationRepository _reservationRepo;

    // Inject repositories needed for the aggregation queries.
    public DashboardService(
        ProsumerRepository prosumerRepo,
        ISolarStationRepository nodeRepo,
        IReservationRepository reservationRepo)
    {
        _prosumerRepo = prosumerRepo;
        _nodeRepo = nodeRepo;
        _reservationRepo = reservationRepo;
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

    // Computes dynamic active and pending reservation counts, next upcoming booking, and recent history.
    public async Task<ProsumerDashboardResponse> GetProsumerDashboardAsync(string nic)
    {
        // Query reservation metrics and historical activity from repository.
        var now = DateTime.UtcNow;
        var (activeCount, pendingCount, nextReservation, recentHistory) =
            await _reservationRepo.GetDashboardMetricsAsync(nic, now);

        var nextDto = nextReservation != null ? MapToResponse(nextReservation) : null;
        var historyDtos = recentHistory.Select(MapToResponse).ToList();

        return new ProsumerDashboardResponse(activeCount, pendingCount, nextDto, historyDtos);
    }

    // Maps a domain EnergyReservation document to a response DTO.
    private static ReservationResponse MapToResponse(EnergyReservation r)
    {
        // Convert internal EnergyReservation entity into response DTO.
        return new ReservationResponse(
            r.ReservationId,
            r.Nic,
            r.NodeId,
            r.SlotId,
            r.SlotStartTime,
            r.SlotEndTime,
            r.RequestedKwh,
            r.Status,
            r.QrToken,
            r.QrIssuedAt,
            r.CreatedAt,
            r.UpdatedAt,
            r.CancelledAt,
            r.CancelledBy,
            r.CompletedAt,
            r.CompletedByOperatorId,
            r.DeliveredKwh
        );
    }
}
