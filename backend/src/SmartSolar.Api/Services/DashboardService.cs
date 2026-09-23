// ---------------------------------------------------------------------------
//  File        : DashboardService.cs
//  Project     : Smart Solar Microgrid Trading System - Web API
//  Description : Aggregates dynamic metrics and summary cards for the prosumer
//                mobile dashboard (REQ-VIEW §2.1, DOM-03).
// ---------------------------------------------------------------------------

using SmartSolar.Api.DTOs.Response.Dashboard;
using SmartSolar.Api.DTOs.Response.Reservations;
using SmartSolar.Api.Models;
using SmartSolar.Api.Repositories;

namespace SmartSolar.Api.Services;

public class DashboardService
{
    private readonly IReservationRepository _reservationRepo;
    private readonly ProsumerRepository? _prosumerRepo;
    private readonly ISolarStationRepository? _nodeRepo;

    // Injects the reservation repository for metric aggregations.
    public DashboardService(IReservationRepository reservationRepo)
    {
        _reservationRepo = reservationRepo;
    }

    // Full constructor injecting repositories for both prosumer and backoffice dashboards.
    public DashboardService(
        IReservationRepository reservationRepo,
        ProsumerRepository prosumerRepo,
        ISolarStationRepository nodeRepo)
    {
        _reservationRepo = reservationRepo;
        _prosumerRepo = prosumerRepo;
        _nodeRepo = nodeRepo;
    }

    // Computes system-wide aggregate metrics for the backoffice dashboard (REQ-VIEW §2.2).
    public async Task<BackofficeDashboardResponse> GetBackofficeDashboardAsync()
    {
        var totalProsumers = _prosumerRepo != null ? await _prosumerRepo.CountTotalAsync() : 0;
        var pendingActivations = _prosumerRepo != null ? await _prosumerRepo.CountByStatusAsync("PendingActivation") : 0;
        var activeNodes = _nodeRepo != null ? await _nodeRepo.CountActiveAsync() : 0;
        var (totalReservations, byStatus) = await _reservationRepo.GetStatusCountsAsync();

        return new BackofficeDashboardResponse(
            totalProsumers,
            pendingActivations,
            activeNodes,
            totalReservations,
            byStatus
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
