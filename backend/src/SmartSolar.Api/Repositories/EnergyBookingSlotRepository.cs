// ---------------------------------------------------------------------------
//  File        : EnergyBookingSlotRepository.cs
//  Project     : Smart Solar Microgrid Trading System - Web API
//  Description : Mongo-backed implementation of IEnergyBookingSlotRepository.
// ---------------------------------------------------------------------------

using MongoDB.Driver;
using SmartSolar.Api.Models;

namespace SmartSolar.Api.Repositories;

public class EnergyBookingSlotRepository : IEnergyBookingSlotRepository
{
    private readonly IMongoCollection<EnergyBookingSlot> _collection;

    // Resolve the collection from the shared database singleton. Indexes are
    // created centrally by MongoIndexInitializer, not per-repository.
    public EnergyBookingSlotRepository(IMongoDatabase database)
    {
        _collection = database.GetCollection<EnergyBookingSlot>("EnergyBookingSlots");
    }

    public async Task<EnergyBookingSlot?> GetBySlotIdAsync(string slotId)
    {
        // Plain equality filter - the unique index on slotId makes this an index hit.
        var filter = Builders<EnergyBookingSlot>.Filter.Eq(s => s.SlotId, slotId);
        return await _collection.Find(filter).FirstOrDefaultAsync();
    }

    public async Task<List<EnergyBookingSlot>> GetByNodeIdAsync(string nodeId, DateTime? from, DateTime? to)
    {
        // Build up the range filter only from the bounds the caller actually supplied.
        var filters = new List<FilterDefinition<EnergyBookingSlot>>
        {
            Builders<EnergyBookingSlot>.Filter.Eq(s => s.NodeId, nodeId),
        };

        if (from.HasValue)
            filters.Add(Builders<EnergyBookingSlot>.Filter.Gte(s => s.StartTime, from.Value));

        if (to.HasValue)
            filters.Add(Builders<EnergyBookingSlot>.Filter.Lt(s => s.StartTime, to.Value));

        var filter = Builders<EnergyBookingSlot>.Filter.And(filters);
        return await _collection.Find(filter).SortBy(s => s.StartTime).ToListAsync();
    }

    public async Task<List<EnergyBookingSlot>> GetOverlappingAsync(
        string nodeId, DateTime startTime, DateTime endTime, string? excludeSlotId)
    {
        // Two windows overlap when each starts before the other ends (classic interval test).
        var filters = new List<FilterDefinition<EnergyBookingSlot>>
        {
            Builders<EnergyBookingSlot>.Filter.Eq(s => s.NodeId, nodeId),
            Builders<EnergyBookingSlot>.Filter.Eq(s => s.IsActive, true),
            Builders<EnergyBookingSlot>.Filter.Lt(s => s.StartTime, endTime),
            Builders<EnergyBookingSlot>.Filter.Gt(s => s.EndTime, startTime),
        };

        if (excludeSlotId != null)
            filters.Add(Builders<EnergyBookingSlot>.Filter.Ne(s => s.SlotId, excludeSlotId));

        var filter = Builders<EnergyBookingSlot>.Filter.And(filters);
        return await _collection.Find(filter).ToListAsync();
    }

    public async Task CreateAsync(EnergyBookingSlot slot)
    {
        // Insert. The unique index on slotId is the final guard against a duplicate.
        await _collection.InsertOneAsync(slot);
    }

    public async Task UpdateAsync(EnergyBookingSlot slot)
    {
        // Replace the whole document, matched on SlotId.
        var filter = Builders<EnergyBookingSlot>.Filter.Eq(s => s.SlotId, slot.SlotId);
        await _collection.ReplaceOneAsync(filter, slot);
    }

    // Atomically decrements availableCapacity if availableCapacity > 0.
    public async Task<bool> TryDecrementCapacityAsync(string slotId)
    {
        // Atomically decrement available slot capacity using filter guard.
        var filter = Builders<EnergyBookingSlot>.Filter.And(
            Builders<EnergyBookingSlot>.Filter.Eq(s => s.SlotId, slotId),
            Builders<EnergyBookingSlot>.Filter.Gt(s => s.AvailableCapacity, 0));

        var update = Builders<EnergyBookingSlot>.Update
            .Inc(s => s.AvailableCapacity, -1)
            .Set(s => s.UpdatedAt, DateTime.UtcNow);

        var result = await _collection.UpdateOneAsync(filter, update);
        return result.ModifiedCount > 0;
    }

    // Atomically restores availableCapacity by 1 upon cancellation of an approved booking.
    public async Task IncrementCapacityAsync(string slotId)
    {
        // Increment available slot capacity by one.
        var filter = Builders<EnergyBookingSlot>.Filter.Eq(s => s.SlotId, slotId);

        var update = Builders<EnergyBookingSlot>.Update
            .Inc(s => s.AvailableCapacity, 1)
            .Set(s => s.UpdatedAt, DateTime.UtcNow);

        await _collection.UpdateOneAsync(filter, update);
    }
}
