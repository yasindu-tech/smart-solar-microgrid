// ---------------------------------------------------------------------------
//  File        : SolarStationRepository.cs
//  Project     : Smart Solar Microgrid Trading System - Web API
//  Description : Mongo-backed implementation of ISolarStationRepository.
// ---------------------------------------------------------------------------

using MongoDB.Driver;
using SmartSolar.Api.Models;

namespace SmartSolar.Api.Repositories;

public class SolarStationRepository : ISolarStationRepository
{
    private readonly IMongoCollection<SolarStationInfo> _collection;

    // Resolve the collection from the shared database singleton. Indexes are
    // created centrally by MongoIndexInitializer, not per-repository.
    public SolarStationRepository(IMongoDatabase database)
    {
        _collection = database.GetCollection<SolarStationInfo>("SolarStationInfo");
    }

    public async Task<SolarStationInfo?> GetByNodeIdAsync(string nodeId)
    {
        // Plain equality filter - the unique index on nodeId makes this an index hit.
        var filter = Builders<SolarStationInfo>.Filter.Eq(n => n.NodeId, nodeId);
        return await _collection.Find(filter).FirstOrDefaultAsync();
    }

    public async Task<(List<SolarStationInfo> Items, int Total)> GetAllAsync(bool? active, int page, int pageSize)
    {
        // Empty filter when active is null, otherwise filter on IsActive.
        var filter = active.HasValue
            ? Builders<SolarStationInfo>.Filter.Eq(n => n.IsActive, active.Value)
            : Builders<SolarStationInfo>.Filter.Empty;

        var total = (int)await _collection.CountDocumentsAsync(filter);
        var items = await _collection.Find(filter)
            .SortBy(n => n.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Limit(pageSize)
            .ToListAsync();

        return (items, total);
    }

    public async Task CreateAsync(SolarStationInfo node)
    {
        // Insert. The unique index on nodeId is the final guard against a duplicate.
        await _collection.InsertOneAsync(node);
    }

    public async Task UpdateAsync(SolarStationInfo node)
    {
        // Replace the whole document, matched on NodeId.
        var filter = Builders<SolarStationInfo>.Filter.Eq(n => n.NodeId, node.NodeId);
        await _collection.ReplaceOneAsync(filter, node);
    }

    public async Task<bool> ExistsAsync(string nodeId)
    {
        // Existence check - a count avoids fetching the whole document.
        var filter = Builders<SolarStationInfo>.Filter.Eq(n => n.NodeId, nodeId);
        return await _collection.CountDocumentsAsync(filter) > 0;
    }

    public async Task<int> CountActiveAsync()
    {
        // Count of IsActive = true, for the Backoffice dashboard.
        var filter = Builders<SolarStationInfo>.Filter.Eq(n => n.IsActive, true);
        return (int)await _collection.CountDocumentsAsync(filter);
    }
}
