// ---------------------------------------------------------------------------
//  NodeRepository.cs - Mongo-backed implementation of INodeRepository.
//  REQ-NODE - Microgrid Node Management (data-model.md 1.3)
// ---------------------------------------------------------------------------

using Microsoft.Extensions.Options;
using MongoDB.Driver;
using SmartSolar.Api.Configuration;
using SmartSolar.Api.Models;

namespace SmartSolar.Api.Repositories;

public class NodeRepository : INodeRepository
{
    private readonly IMongoCollection<Node> _collection;

    // Resolve the collection once at construction; IMongoClient is thread-safe.
    public NodeRepository(IOptions<MongoSettings> settings)
    {
        var client = new MongoClient(settings.Value.ConnectionString);
        var db = client.GetDatabase(settings.Value.DatabaseName);
        _collection = db.GetCollection<Node>("SolarStationInfo");

        // Unique index on nodeId, plus an index on isActive for the list/filter query.
        var uniqueOptions = new CreateIndexOptions { Unique = true };
        _collection.Indexes.CreateOne(new CreateIndexModel<Node>(
            Builders<Node>.IndexKeys.Ascending(n => n.NodeId), uniqueOptions));
        _collection.Indexes.CreateOne(new CreateIndexModel<Node>(
            Builders<Node>.IndexKeys.Ascending(n => n.IsActive)));
    }

    // Plain equality filter - the unique index on nodeId makes this an index hit.
    public async Task<Node?> GetByNodeIdAsync(string nodeId)
    {
        var filter = Builders<Node>.Filter.Eq(n => n.NodeId, nodeId);
        return await _collection.Find(filter).FirstOrDefaultAsync();
    }

    // Build an empty filter when active is null, otherwise filter on IsActive.
    // Count first, then Find + Skip + Limit, sorted by CreatedAt for stable paging.
    public async Task<(List<Node> Items, int Total)> GetAllAsync(bool? active, int page, int pageSize)
    {
        var filter = active.HasValue
            ? Builders<Node>.Filter.Eq(n => n.IsActive, active.Value)
            : Builders<Node>.Filter.Empty;

        var total = (int)await _collection.CountDocumentsAsync(filter);
        var items = await _collection.Find(filter)
            .SortBy(n => n.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Limit(pageSize)
            .ToListAsync();

        return (items, total);
    }

    // Insert. The unique index on nodeId is the final guard against a duplicate.
    public async Task CreateAsync(Node node)
    {
        await _collection.InsertOneAsync(node);
    }

    // Replace the whole document, matched on NodeId.
    public async Task UpdateAsync(Node node)
    {
        var filter = Builders<Node>.Filter.Eq(n => n.NodeId, node.NodeId);
        await _collection.ReplaceOneAsync(filter, node);
    }

    // Existence check for D1's assignedNodeIds validation - a count avoids fetching the document.
    public async Task<bool> ExistsAsync(string nodeId)
    {
        var filter = Builders<Node>.Filter.Eq(n => n.NodeId, nodeId);
        return await _collection.CountDocumentsAsync(filter) > 0;
    }

    // Count of IsActive = true, for the Backoffice dashboard.
    public async Task<int> CountActiveAsync()
    {
        var filter = Builders<Node>.Filter.Eq(n => n.IsActive, true);
        return (int)await _collection.CountDocumentsAsync(filter);
    }
}
