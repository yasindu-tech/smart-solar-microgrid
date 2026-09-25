// ---------------------------------------------------------------------------
//  File        : DashboardService.cs
//  Project     : Smart Solar Microgrid Trading System - Web API
//  Description : Aggregates dynamic metrics and summary cards for the prosumer
//                mobile dashboard (REQ-VIEW §2.1, DOM-03), the grid operator
//                dashboard scoped to assigned nodes (REQ-VIEW §2.3, DOM-04),
//                and backoffice dashboard (REQ-VIEW §2.4).
// ---------------------------------------------------------------------------

using SmartSolar.Api.DTOs.Response.Dashboard;
using SmartSolar.Api.DTOs.Response.Operator;
using SmartSolar.Api.Middleware;
using SmartSolar.Api.DTOs.Response.Reservations;
using SmartSolar.Api.Models;
using SmartSolar.Api.Repositories;

namespace SmartSolar.Api.Services;

public class DashboardService
{
    private readonly ProsumerRepository _prosumerRepo;
    private readonly ISolarStationRepository _nodeRepo;
    private readonly IReservationRepository _reservationRepo;
    private readonly IUserRepository _userRepo;

    // Injects the repositories needed across the prosumer, backoffice and operator dashboards.
    public DashboardService(
        IReservationRepository reservationRepo,
        ProsumerRepository prosumerRepo,
        ISolarStationRepository nodeRepo,
        IUserRepository userRepo)
    {
        _reservationRepo = reservationRepo;
        _prosumerRepo = prosumerRepo;
        _nodeRepo = nodeRepo;
        _userRepo = userRepo;
    }

    // Computes system-wide aggregate metrics for the backoffice dashboard (REQ-VIEW §2.2).
    public async Task<BackofficeDashboardResponse> GetBackofficeDashboardAsync()
    {
        // Fire all count queries in parallel for faster response.
        var totalProsumersTask = _prosumerRepo != null ? _prosumerRepo.CountTotalAsync() : Task.FromResult(0);
        var pendingActivationsTask = _prosumerRepo != null ? _prosumerRepo.CountByStatusAsync("PendingActivation") : Task.FromResult(0);
        var activeNodesTask = _nodeRepo != null ? _nodeRepo.CountActiveAsync() : Task.FromResult(0);
        var reservationMetricsTask = _reservationRepo != null ? _reservationRepo.GetStatusCountsAsync() : Task.FromResult((0, new Dictionary<string, int>()));

        await Task.WhenAll(totalProsumersTask, pendingActivationsTask, activeNodesTask, reservationMetricsTask);

        var (totalReservations, byStatus) = reservationMetricsTask.Result;

        return new BackofficeDashboardResponse(
            totalProsumersTask.Result,
            pendingActivationsTask.Result,
            activeNodesTask.Result,
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

    // Computes operator dashboard counts and today's schedule, scoped to the caller's assigned nodes (BR-11).
    public async Task<OperatorDashboardResponse> GetOperatorDashboardAsync(string callerUserId, string? nodeIdFilter)
    {
        // Resolve the calling operator and the node scope every count below is bounded by.
        var now = DateTime.UtcNow;

        var caller = await _userRepo.GetByUserIdAsync(callerUserId);
        if (caller == null)
            throw new DomainException("E4404", "Operator account not found.", 404, "userId");

        var scopedNodeIds = caller.AssignedNodeIds
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        // BR-11: an explicit ?nodeId= may only narrow the operator's own scope, never widen it.
        if (!string.IsNullOrWhiteSpace(nodeIdFilter))
        {
            var requestedNodeId = nodeIdFilter.Trim();
            if (!scopedNodeIds.Contains(requestedNodeId, StringComparer.OrdinalIgnoreCase))
                throw new DomainException("E4303", "You are not assigned to this node.", 403, "nodeId");

            scopedNodeIds = new List<string> { requestedNodeId };
        }

        // "Today" is a whole UTC day, so the list matches what the operator will physically see on shift.
        var dayStart = now.Date;
        var dayEnd = dayStart.AddDays(1);

        var (pendingCount, approvedFutureCount, todaysReservations) =
            await _reservationRepo.GetOperatorDashboardMetricsAsync(scopedNodeIds, now, dayStart, dayEnd);

        var todaysDtos = todaysReservations.Select(r => MapToResponse(r) with { QrToken = null }).ToList();

        return new OperatorDashboardResponse(pendingCount, approvedFutureCount, todaysDtos, scopedNodeIds);
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
