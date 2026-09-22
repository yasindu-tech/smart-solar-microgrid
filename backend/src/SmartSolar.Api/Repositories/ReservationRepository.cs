// ---------------------------------------------------------------------------
//  File        : ReservationRepository.cs
//  Project     : Smart Solar Microgrid Trading System - Web API
//  Description : MongoDB repository implementation for managing reservations
//                and querying prosumer and staff views (DOM-03).
// ---------------------------------------------------------------------------

using System.Text.RegularExpressions;
using MongoDB.Bson;
using MongoDB.Driver;
using SmartSolar.Api.Models;

namespace SmartSolar.Api.Repositories;

public class ReservationRepository : IReservationRepository
{
    private readonly IMongoCollection<EnergyReservation> _collection;

    // Injects the shared database instance and binds the EnergyReservation collection.
    public ReservationRepository(IMongoDatabase database)
    {
        // Bind the MongoDB EnergyReservation collection.
        _collection = database.GetCollection<EnergyReservation>("EnergyReservation");
    }

    // Retrieves a single reservation by its unique reservation identifier.
    public async Task<EnergyReservation?> GetByIdAsync(string reservationId)
    {
        // Query reservation document by unique identifier.
        var filter = Builders<EnergyReservation>.Filter.Eq(r => r.ReservationId, reservationId);
        return await _collection.Find(filter).FirstOrDefaultAsync();
    }

    // Queries reservations for a specific prosumer with pagination and status/history filtering.
    public async Task<(List<EnergyReservation> Items, int Total)> GetMineAsync(
        string nic, string? status, int page, int pageSize)
    {
        // Build query filters for caller's reservations.
        var filters = new List<FilterDefinition<EnergyReservation>>
        {
            Builders<EnergyReservation>.Filter.Eq(r => r.Nic, nic)
        };

        if (!string.IsNullOrWhiteSpace(status))
        {
            if (string.Equals(status.Trim(), "history", StringComparison.OrdinalIgnoreCase))
            {
                var now = DateTime.UtcNow;
                filters.Add(Builders<EnergyReservation>.Filter.Or(
                    Builders<EnergyReservation>.Filter.In(r => r.Status, new[] { "Completed", "Cancelled", "Expired" }),
                    Builders<EnergyReservation>.Filter.Lt(r => r.SlotEndTime, now)));
            }
            else
            {
                filters.Add(Builders<EnergyReservation>.Filter.Eq(r => r.Status, status.Trim()));
            }
        }

        var combinedFilter = Builders<EnergyReservation>.Filter.And(filters);
        var total = (int)await _collection.CountDocumentsAsync(combinedFilter);

        var items = await _collection.Find(combinedFilter)
            .SortByDescending(r => r.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Limit(pageSize)
            .ToListAsync();

        return (items, total);
    }

    // Queries reservations with staff filters (nodeId, status, date range, search).
    public async Task<(List<EnergyReservation> Items, int Total)> GetAllAsync(
        string? status, string? nodeId, DateTime? from, DateTime? to, string? search, int page, int pageSize)
    {
        // Assemble dynamic filtering criteria for reservations.
        var filters = new List<FilterDefinition<EnergyReservation>>();

        if (!string.IsNullOrWhiteSpace(status))
        {
            filters.Add(Builders<EnergyReservation>.Filter.Eq(r => r.Status, status.Trim()));
        }

        if (!string.IsNullOrWhiteSpace(nodeId))
        {
            filters.Add(Builders<EnergyReservation>.Filter.Eq(r => r.NodeId, nodeId.Trim()));
        }

        if (from.HasValue)
        {
            filters.Add(Builders<EnergyReservation>.Filter.Gte(r => r.SlotStartTime, from.Value));
        }

        if (to.HasValue)
        {
            filters.Add(Builders<EnergyReservation>.Filter.Lte(r => r.SlotStartTime, to.Value));
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var searchRegex = new BsonRegularExpression(Regex.Escape(search.Trim()), "i");
            filters.Add(Builders<EnergyReservation>.Filter.Or(
                Builders<EnergyReservation>.Filter.Regex(r => r.ReservationId, searchRegex),
                Builders<EnergyReservation>.Filter.Regex(r => r.Nic, searchRegex)));
        }

        var combinedFilter = filters.Count > 0
            ? Builders<EnergyReservation>.Filter.And(filters)
            : Builders<EnergyReservation>.Filter.Empty;

        var total = (int)await _collection.CountDocumentsAsync(combinedFilter);

        var items = await _collection.Find(combinedFilter)
            .SortByDescending(r => r.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Limit(pageSize)
            .ToListAsync();

        return (items, total);
    }

    // Inserts a new reservation document.
    public async Task CreateAsync(EnergyReservation reservation)
    {
        // Insert a new energy reservation document into the collection.
        await _collection.InsertOneAsync(reservation);
    }

    // Replaces an existing reservation document by reservationId.
    public async Task UpdateAsync(EnergyReservation reservation)
    {
        // Replace existing energy reservation document matching its reservation ID.
        var filter = Builders<EnergyReservation>.Filter.Eq(r => r.ReservationId, reservation.ReservationId);
        await _collection.ReplaceOneAsync(filter, reservation);
    }

    // Aggregates dashboard metrics (active, pending, next, recent history) for a prosumer.
    public async Task<(int ActiveCount, int PendingCount, EnergyReservation? NextReservation, List<EnergyReservation> RecentHistory)>
        GetDashboardMetricsAsync(string nic, DateTime nowUtc)
    {
        // Execute aggregated queries for active and pending reservations and recent activity.
        var activeFilter = Builders<EnergyReservation>.Filter.And(
            Builders<EnergyReservation>.Filter.Eq(r => r.Nic, nic),
            Builders<EnergyReservation>.Filter.Eq(r => r.Status, "Approved"),
            Builders<EnergyReservation>.Filter.Gte(r => r.SlotStartTime, nowUtc));

        var pendingFilter = Builders<EnergyReservation>.Filter.And(
            Builders<EnergyReservation>.Filter.Eq(r => r.Nic, nic),
            Builders<EnergyReservation>.Filter.Eq(r => r.Status, "Pending"));

        var activeCountTask = _collection.CountDocumentsAsync(activeFilter);
        var pendingCountTask = _collection.CountDocumentsAsync(pendingFilter);

        var upcomingFilter = Builders<EnergyReservation>.Filter.And(
            Builders<EnergyReservation>.Filter.Eq(r => r.Nic, nic),
            Builders<EnergyReservation>.Filter.In(r => r.Status, new[] { "Pending", "Approved" }),
            Builders<EnergyReservation>.Filter.Gte(r => r.SlotStartTime, nowUtc));

        var nextReservationTask = _collection.Find(upcomingFilter)
            .SortBy(r => r.SlotStartTime)
            .FirstOrDefaultAsync();

        var historyFilter = Builders<EnergyReservation>.Filter.And(
            Builders<EnergyReservation>.Filter.Eq(r => r.Nic, nic),
            Builders<EnergyReservation>.Filter.Or(
                Builders<EnergyReservation>.Filter.In(r => r.Status, new[] { "Completed", "Cancelled", "Expired" }),
                Builders<EnergyReservation>.Filter.Lt(r => r.SlotEndTime, nowUtc)));

        var recentHistoryTask = _collection.Find(historyFilter)
            .SortByDescending(r => r.CreatedAt)
            .Limit(5)
            .ToListAsync();

        await Task.WhenAll(activeCountTask, pendingCountTask, nextReservationTask, recentHistoryTask);

        return (
            (int)await activeCountTask,
            (int)await pendingCountTask,
            await nextReservationTask,
            await recentHistoryTask
        );
    }
}
