// ---------------------------------------------------------------------------
//  File        : UserRepository.cs
//  Project     : Smart Solar Microgrid Trading System - Web API
//  Module      : Repositories
//  Description : MongoDB access layer for the Users collection. All queries
//                go through this class so controllers and services never hold
//                a direct IMongoCollection reference.
//  Author      : <member name - M1>
//  Created     : 2026-09-16
// ---------------------------------------------------------------------------

using Microsoft.Extensions.Options;
using MongoDB.Driver;
using SmartSolar.Api.Configuration;
using SmartSolar.Api.Models;

namespace SmartSolar.Api.Repositories;

public class UserRepository
{
    private readonly IMongoCollection<User> _collection;

    // Resolve the collection once at construction; IMongoClient is thread-safe.
    public UserRepository(IOptions<MongoSettings> settings)
    {
        var client = new MongoClient(settings.Value.ConnectionString);
        var db = client.GetDatabase(settings.Value.DatabaseName);
        _collection = db.GetCollection<User>("Users");
    }

    // Find a user by their email address (case-insensitive). Used at login.
    public async Task<User?> GetByEmailAsync(string email)
    {
        var filter = Builders<User>.Filter.Regex(
            u => u.Email,
            new MongoDB.Bson.BsonRegularExpression($"^{System.Text.RegularExpressions.Regex.Escape(email)}$", "i"));
        return await _collection.Find(filter).FirstOrDefaultAsync();
    }

    // Find a user by their sequential UserId (e.g. "USR-0001").
    public async Task<User?> GetByUserIdAsync(string userId)
    {
        var filter = Builders<User>.Filter.Eq(u => u.UserId, userId);
        return await _collection.Find(filter).FirstOrDefaultAsync();
    }

    // Paginated list with optional role filter. Used by GET /api/users.
    public async Task<(List<User> Items, int Total)> GetAllAsync(
        string? role, int page, int pageSize)
    {
        // Build filter — if role is omitted return all roles.
        var filter = role != null
            ? Builders<User>.Filter.Eq(u => u.Role, role)
            : Builders<User>.Filter.Empty;

        var total = (int)await _collection.CountDocumentsAsync(filter);
        var items = await _collection.Find(filter)
            .SortBy(u => u.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Limit(pageSize)
            .ToListAsync();

        return (items, total);
    }

    // Insert a new user document.
    public async Task CreateAsync(User user)
    {
        await _collection.InsertOneAsync(user);
    }

    // Replace an existing user document by UserId.
    public async Task UpdateAsync(User user)
    {
        var filter = Builders<User>.Filter.Eq(u => u.UserId, user.UserId);
        await _collection.ReplaceOneAsync(filter, user);
    }

    // Count active Backoffice accounts — used to guard against deactivating
    // the last Backoffice (REQ-USER EC-2).
    public async Task<int> CountActiveBackofficeAsync()
    {
        var filter = Builders<User>.Filter.And(
            Builders<User>.Filter.Eq(u => u.Role, "Backoffice"),
            Builders<User>.Filter.Eq(u => u.IsActive, true));
        return (int)await _collection.CountDocumentsAsync(filter);
    }

    // Check whether an email is already registered (excluding a given userId
    // to allow a user to keep their own email on update).
    public async Task<bool> EmailExistsAsync(string email, string? excludeUserId = null)
    {
        var emailFilter = Builders<User>.Filter.Regex(
            u => u.Email,
            new MongoDB.Bson.BsonRegularExpression($"^{System.Text.RegularExpressions.Regex.Escape(email)}$", "i"));

        if (excludeUserId != null)
        {
            var notSelf = Builders<User>.Filter.Ne(u => u.UserId, excludeUserId);
            emailFilter = Builders<User>.Filter.And(emailFilter, notSelf);
        }

        return await _collection.CountDocumentsAsync(emailFilter) > 0;
    }

    // Generate the next sequential UserId in the form "USR-NNNN".
    public async Task<string> GetNextUserIdAsync()
    {
        // Find the highest existing number by sorting descending on userId.
        var latest = await _collection.Find(Builders<User>.Filter.Empty)
            .SortByDescending(u => u.UserId)
            .Limit(1)
            .FirstOrDefaultAsync();

        if (latest == null) return "USR-0001";

        // Parse the numeric suffix and increment.
        var parts = latest.UserId.Split('-');
        var next = int.Parse(parts[1]) + 1;
        return $"USR-{next:D4}";
    }
}
