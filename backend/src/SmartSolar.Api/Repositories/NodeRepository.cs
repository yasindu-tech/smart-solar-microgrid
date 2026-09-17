using Microsoft.Extensions.Options;
using MongoDB.Driver;
using SmartSolar.Api.Configuration;
using SmartSolar.Api.Models;

namespace SmartSolar.Api.Repositories;

public class NodeRepository : INodeRepository
{
    private readonly IMongoCollection<Node> _collection;

    public NodeRepository(IOptions<MongoSettings> settings)
    {
        var client = new MongoClient(settings.Value.ConnectionString);
        var db = client.GetDatabase(settings.Value.DatabaseName);
        _collection = db.GetCollection<Node>("SolarStationInfo");

        var uniqueOptions = new CreateIndexOptions { Unique = true };
        _collection.Indexes.CreateOne(new CreateIndexModel<Node>(
            Builders<Node>.IndexKeys.Ascending(n => n.NodeId), uniqueOptions));
        _collection.Indexes.CreateOne(new CreateIndexModel<Node>(
            Builders<Node>.IndexKeys.Ascending(n => n.IsActive)));
    }

    public async Task<Node?> GetByNodeIdAsync(string nodeId)
    {
        var filter = Builders<Node>.Filter.Eq(n => n.NodeId, nodeId);
        return await _collection.Find(filter).FirstOrDefaultAsync();
    }

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

    public async Task CreateAsync(Node node)
    {
        await _collection.InsertOneAsync(node);
    }

    public async Task UpdateAsync(Node node)
    {
        var filter = Builders<Node>.Filter.Eq(n => n.NodeId, node.NodeId);
        await _collection.ReplaceOneAsync(filter, node);
    }

    public async Task<bool> ExistsAsync(string nodeId)
    {
        var filter = Builders<Node>.Filter.Eq(n => n.NodeId, nodeId);
        return await _collection.CountDocumentsAsync(filter) > 0;
    }

    public async Task<int> CountActiveAsync()
    {
        var filter = Builders<Node>.Filter.Eq(n => n.IsActive, true);
        return (int)await _collection.CountDocumentsAsync(filter);
    }
}
