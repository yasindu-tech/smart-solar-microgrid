using Microsoft.Extensions.Options;
using MongoDB.Driver;
using SmartSolar.Api.Configuration;
using SmartSolar.Api.Models;

namespace SmartSolar.Api.Repositories;

public class ProsumerRepository
{
    private readonly IMongoCollection<Prosumer> _collection;

    // Resolve the collection once at construction.
    public ProsumerRepository(IOptions<MongoSettings> settings)
    {
        var client = new MongoClient(settings.Value.ConnectionString);
        var db = client.GetDatabase(settings.Value.DatabaseName);
        _collection = db.GetCollection<Prosumer>("Prosumers");

        // Create indexes
        var uniqueOptions = new CreateIndexOptions { Unique = true };
        _collection.Indexes.CreateOne(new CreateIndexModel<Prosumer>(
            Builders<Prosumer>.IndexKeys.Ascending(p => p.Email), uniqueOptions));

        var collationOptions = new CreateIndexOptions { Collation = new Collation("en", strength: CollationStrength.Secondary) };
        _collection.Indexes.CreateOne(new CreateIndexModel<Prosumer>(
            Builders<Prosumer>.IndexKeys.Ascending(p => p.Nic), collationOptions));
    }

    public async Task<Prosumer?> GetByNicAsync(string nic)
    {
        var filter = Builders<Prosumer>.Filter.Eq(p => p.Nic, nic.Trim());
        var options = new FindOptions<Prosumer> { Collation = new Collation("en", strength: CollationStrength.Secondary) };
        var cursor = await _collection.FindAsync(filter, options);
        return await cursor.FirstOrDefaultAsync();
    }

    // Paginated list with optional status filter and search across NIC/name.
    // Used by GET /api/prosumers.
    public async Task<(List<Prosumer> Items, int Total)> GetAllAsync(
        string? status, string? search, int page, int pageSize)
    {
        // Start with empty filter and layer in optional clauses.
        var filters = new List<FilterDefinition<Prosumer>>();

        if (!string.IsNullOrWhiteSpace(status))
            filters.Add(Builders<Prosumer>.Filter.Eq(p => p.Status, status));

        if (!string.IsNullOrWhiteSpace(search))
        {
            // Partial case-insensitive match on nic or fullName (REQ-PROS AC-9).
            var searchRegex = new MongoDB.Bson.BsonRegularExpression(
                System.Text.RegularExpressions.Regex.Escape(search.Trim()), "i");
            filters.Add(Builders<Prosumer>.Filter.Or(
                Builders<Prosumer>.Filter.Regex(p => p.Nic, searchRegex),
                Builders<Prosumer>.Filter.Regex(p => p.FullName, searchRegex)));
        }

        var combined = filters.Count > 0
            ? Builders<Prosumer>.Filter.And(filters)
            : Builders<Prosumer>.Filter.Empty;

        var total = (int)await _collection.CountDocumentsAsync(combined);
        var items = await _collection.Find(combined)
            .SortByDescending(p => p.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Limit(pageSize)
            .ToListAsync();

        return (items, total);
    }

    // Paginated list of PendingActivation prosumers ordered oldest-first.
    // Used by GET /api/prosumers/pending (REQ-PROS §4.2).
    public async Task<(List<Prosumer> Items, int Total)> GetPendingAsync(int page, int pageSize)
    {
        var filter = Builders<Prosumer>.Filter.Eq(p => p.Status, "PendingActivation");
        var total = (int)await _collection.CountDocumentsAsync(filter);
        var items = await _collection.Find(filter)
            .SortBy(p => p.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Limit(pageSize)
            .ToListAsync();

        return (items, total);
    }

    // Insert a new prosumer document (NIC becomes the _id).
    public async Task CreateAsync(Prosumer prosumer)
    {
        await _collection.InsertOneAsync(prosumer);
    }

    // Replace an existing prosumer document by NIC.
    public async Task UpdateAsync(Prosumer prosumer)
    {
        var filter = Builders<Prosumer>.Filter.Eq(p => p.Id, prosumer.Nic);
        await _collection.ReplaceOneAsync(filter, prosumer);
    }

    // Count prosumers by status. Used for the Backoffice dashboard.
    public async Task<int> CountByStatusAsync(string status)
    {
        var filter = Builders<Prosumer>.Filter.Eq(p => p.Status, status);
        return (int)await _collection.CountDocumentsAsync(filter);
    }

    // Total prosumer count across all statuses. Used for the dashboard.
    public async Task<int> CountTotalAsync()
    {
        return (int)await _collection.CountDocumentsAsync(Builders<Prosumer>.Filter.Empty);
    }

    public async Task<bool> EmailExistsAsync(string email, string? excludeNic = null)
    {
        var emailFilter = Builders<Prosumer>.Filter.Eq(p => p.Email, email.Trim());

        if (excludeNic != null)
        {
            var notSelf = Builders<Prosumer>.Filter.Ne(p => p.Id, excludeNic);
            emailFilter = Builders<Prosumer>.Filter.And(emailFilter, notSelf);
        }

        var options = new CountOptions { Collation = new Collation("en", strength: CollationStrength.Secondary) };
        return await _collection.CountDocumentsAsync(emailFilter, options) > 0;
    }
}
