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

    // Queries reservations for a specific prosumer with pagination and history filtering.
    Task<(List<EnergyReservation> Items, int Total)> GetMineAsync(
        string nic, string? status, int page, int pageSize);

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
}
