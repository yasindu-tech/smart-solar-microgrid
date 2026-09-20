// ---------------------------------------------------------------------------
//  File        : MongoIndexInitializer.cs
//  Project     : Smart Solar Microgrid Trading System - Web API
//  Description : Asynchronous background hosted service for initializing and
//                verifying required MongoDB indexes at application startup.
// ---------------------------------------------------------------------------

using MongoDB.Driver;
using SmartSolar.Api.Models;

namespace SmartSolar.Api.Configuration;

public class MongoIndexInitializer : IHostedService
{
    private readonly IMongoDatabase _database;
    private readonly ILogger<MongoIndexInitializer> _logger;

    // Injects the MongoDB database instance and logging provider.
    public MongoIndexInitializer(IMongoDatabase database, ILogger<MongoIndexInitializer> logger)
    {
        _database = database;
        _logger = logger;
    }

    // Executes index initialization asynchronously during application boot.
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Initializing MongoDB indexes asynchronously...");

        try
        {
            await InitializeUserIndexesAsync(cancellationToken);
            await InitializeProsumerIndexesAsync(cancellationToken);
            await InitializeEnergyBookingSlotsIndexesAsync(cancellationToken);
            await InitializeEnergyReservationIndexesAsync(cancellationToken);
            _logger.LogInformation("MongoDB indexes verified successfully.");
        }
        catch (Exception ex)
        {
            _logger.LogCritical(ex, "Failed to initialize required MongoDB indexes. Aborting application startup to prevent data inconsistency.");
            throw;
        }
    }

    // Ensures unique indexes on the Users collection for email and userId.
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

    // Ensures unique and collation indexes on the Prosumers collection for email, nic, and status.
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

    // Ensures unique and compound indexes on the EnergyBookingSlots collection for slotId and { nodeId: 1, startTime: 1 }.
    private async Task InitializeEnergyBookingSlotsIndexesAsync(CancellationToken cancellationToken)
    {
        var slots = _database.GetCollection<EnergyBookingSlot>("EnergyBookingSlots");

        var slotIdIndex = new CreateIndexModel<EnergyBookingSlot>(
            Builders<EnergyBookingSlot>.IndexKeys.Ascending(s => s.SlotId),
            new CreateIndexOptions { Unique = true });

        var nodeTimeIndex = new CreateIndexModel<EnergyBookingSlot>(
            Builders<EnergyBookingSlot>.IndexKeys
                .Ascending(s => s.NodeId)
                .Ascending(s => s.StartTime));

        await slots.Indexes.CreateManyAsync(new[] { slotIdIndex, nodeTimeIndex }, cancellationToken);
    }

    // Ensures unique and compound indexes on the EnergyReservation collection for reservationId, { nic: 1, status: 1 }, and { nodeId: 1, slotStartTime: 1 }.
    private async Task InitializeEnergyReservationIndexesAsync(CancellationToken cancellationToken)
    {
        var reservations = _database.GetCollection<EnergyReservation>("EnergyReservation");

        var resIdIndex = new CreateIndexModel<EnergyReservation>(
            Builders<EnergyReservation>.IndexKeys.Ascending(r => r.ReservationId),
            new CreateIndexOptions { Unique = true });

        var nicStatusIndex = new CreateIndexModel<EnergyReservation>(
            Builders<EnergyReservation>.IndexKeys
                .Ascending(r => r.Nic)
                .Ascending(r => r.Status));

        var nodeSlotTimeIndex = new CreateIndexModel<EnergyReservation>(
            Builders<EnergyReservation>.IndexKeys
                .Ascending(r => r.NodeId)
                .Ascending(r => r.SlotStartTime));

        await reservations.Indexes.CreateManyAsync(new[] { resIdIndex, nicStatusIndex, nodeSlotTimeIndex }, cancellationToken);
    }

    // Handles graceful shutdown when the host stops.
    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
