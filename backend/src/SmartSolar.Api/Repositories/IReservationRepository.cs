// ---------------------------------------------------------------------------
//  File        : IReservationRepository.cs
//  Project     : Smart Solar Microgrid Trading System - Web API
//  Description : Interface for persistence and querying of the EnergyReservation collection.
// ---------------------------------------------------------------------------

using SmartSolar.Api.Models;

namespace SmartSolar.Api.Repositories;

public interface IReservationRepository
{
    // Retrieves a single reservation by its unique reservation identifier.
    Task<EnergyReservation?> GetByIdAsync(string reservationId);

    // Queries reservations for a specific prosumer with pagination, search, and date range filtering.
    Task<(List<EnergyReservation> Items, int Total)> GetMineAsync(
        string nic, string? status, DateTime? from, DateTime? to, string? search, int page, int pageSize);

    // Queries reservations with staff filters (nodeId, status, date range, search).
    Task<(List<EnergyReservation> Items, int Total)> GetAllAsync(
        string? status, string? nodeId, DateTime? from, DateTime? to, string? search, int page, int pageSize);

    // Inserts a new reservation document.
    Task CreateAsync(EnergyReservation reservation);

    // Replaces an existing reservation document.
    Task UpdateAsync(EnergyReservation reservation);

    // Aggregates dashboard metrics (active, pending, next, recent history) for a prosumer.
    Task<(int ActiveCount, int PendingCount, EnergyReservation? NextReservation, List<EnergyReservation> RecentHistory)>
        GetDashboardMetricsAsync(string nic, DateTime nowUtc);

    // Aggregates operator dashboard metrics scoped to the operator's assigned nodes (REQ-VIEW §2.3, BR-11).
    Task<(int PendingCount, int ApprovedFutureCount, List<EnergyReservation> TodaysReservations)>
        GetOperatorDashboardMetricsAsync(IEnumerable<string> nodeIds, DateTime nowUtc, DateTime dayStartUtc, DateTime dayEndUtc);

    // Atomically moves a Pending reservation to Approved and sets QR token. Returns false if not Pending.
    Task<bool> TryApproveAsync(string reservationId, string qrToken, DateTime qrIssuedAt);

    // Reverts an Approved reservation back to Pending if slot capacity decrement fails.
    Task<bool> RevertApprovalAsync(string reservationId, DateTime updatedAt);

    // Atomically moves a Pending or Approved reservation to Cancelled. Returns (true, previousStatus) on success.
    Task<(bool Success, string? PreviousStatus)> TryCancelAsync(string reservationId, string cancelledBy, DateTime cancelledAt);

    // Atomically moves an Approved reservation whose stored token still matches to Completed (BR-10, BR-18).
    // Returns false when another operator won the race or the token is stale - the caller then re-reads to pick the error code.
    Task<bool> TryCompleteAsync(
        string reservationId, string presentedToken, string operatorUserId, double deliveredKwh, DateTime completedAt);

    // Atomically moves a past Pending or Approved reservation to Expired (ADR-001). Returns (true, previousStatus) on success.
    Task<(bool Success, string? PreviousStatus)> TryExpireAsync(string reservationId, DateTime nowUtc);

    // Aggregates total reservations count and grouped status counts for the backoffice dashboard.
    Task<(int Total, Dictionary<string, int> ByStatus)> GetStatusCountsAsync();
}

