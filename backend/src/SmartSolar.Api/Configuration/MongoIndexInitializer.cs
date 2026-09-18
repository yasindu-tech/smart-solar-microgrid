using MongoDB.Driver;
using SmartSolar.Api.Models;

namespace SmartSolar.Api.Configuration;

public class MongoIndexInitializer : IHostedService
{
    private readonly IMongoDatabase _database;
    private readonly ILogger<MongoIndexInitializer> _logger;

    public MongoIndexInitializer(IMongoDatabase database, ILogger<MongoIndexInitializer> logger)
    {
        _database = database;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Initializing MongoDB indexes asynchronously...");

        try
        {
            await InitializeUserIndexesAsync(cancellationToken);
            await InitializeProsumerIndexesAsync(cancellationToken);
            _logger.LogInformation("MongoDB indexes verified successfully.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize one or more MongoDB indexes.");
        }
    }

    private async Task InitializeUserIndexesAsync(CancellationToken cancellationToken)
    {
        var users = _database.GetCollection<User>("Users");
        var uniqueOptions = new CreateIndexOptions { Unique = true };

        var emailIndex = new CreateIndexModel<User>(
            Builders<User>.IndexKeys.Ascending(u => u.Email), uniqueOptions);
        var userIdIndex = new CreateIndexModel<User>(
            Builders<User>.IndexKeys.Ascending(u => u.UserId), uniqueOptions);

        await users.Indexes.CreateManyAsync(new[] { emailIndex, userIdIndex }, cancellationToken);
    }

    private async Task InitializeProsumerIndexesAsync(CancellationToken cancellationToken)
    {
        var prosumers = _database.GetCollection<Prosumer>("Prosumers");

        var emailIndex = new CreateIndexModel<Prosumer>(
            Builders<Prosumer>.IndexKeys.Ascending(p => p.Email),
            new CreateIndexOptions { Unique = true });

        var statusIndex = new CreateIndexModel<Prosumer>(
            Builders<Prosumer>.IndexKeys.Ascending(p => p.Status));

        var nicCollationOptions = new CreateIndexOptions
        {
            Unique = true,
            Collation = new Collation("en", strength: CollationStrength.Secondary)
        };
        var nicIndex = new CreateIndexModel<Prosumer>(
            Builders<Prosumer>.IndexKeys.Ascending(p => p.Nic),
            nicCollationOptions);

        await prosumers.Indexes.CreateOneAsync(emailIndex, cancellationToken: cancellationToken);
        await prosumers.Indexes.CreateOneAsync(statusIndex, cancellationToken: cancellationToken);

        try
        {
            await prosumers.Indexes.CreateOneAsync(nicIndex, cancellationToken: cancellationToken);
        }
        catch (MongoCommandException ex) when (ex.Code == 85 || ex.Message.Contains("IndexOptionsConflict"))
        {
            _logger.LogWarning("Dropping existing conflicting 'nic_1' index to apply updated collation/unique options...");
            await prosumers.Indexes.DropOneAsync("nic_1", cancellationToken);
            await prosumers.Indexes.CreateOneAsync(nicIndex, cancellationToken: cancellationToken);
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
