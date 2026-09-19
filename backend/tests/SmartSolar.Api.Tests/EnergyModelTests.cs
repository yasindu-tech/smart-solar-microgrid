// ---------------------------------------------------------------------------
//  File        : EnergyModelTests.cs
//  Project     : Smart Solar Microgrid Trading System - Web API tests
//  Module      : Tests
//  Description : Unit tests verifying field mapping and BSON serialization
//                for EnergyBookingSlot and EnergyReservation models (DOM-03).
//  Author      : Member 3
//  Created     : 2026-09-19
// ---------------------------------------------------------------------------

using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using SmartSolar.Api.Models;
using Xunit;

namespace SmartSolar.Api.Tests;

public class EnergyModelTests
{
    // Verifies that EnergyBookingSlot serializes and deserializes correctly with MongoDB BSON.
    [Fact]
    public void EnergyBookingSlot_BsonSerialization_RoundTripsSuccessfully()
    {
        var now = DateTime.UtcNow;
        var slot = new EnergyBookingSlot
        {
            SlotId = "SL-0042",
            NodeId = "ND-003",
            StartTime = now,
            EndTime = now.AddHours(2),
            TotalCapacity = 4,
            AvailableCapacity = 3,
            EnergyOfferedKwh = 20.0,
            IsActive = true,
            CreatedAt = now,
            UpdatedAt = now
        };

        var bsonDoc = slot.ToBsonDocument();
        Assert.Equal("SL-0042", bsonDoc["slotId"].AsString);
        Assert.Equal("ND-003", bsonDoc["nodeId"].AsString);
        Assert.Equal(4, bsonDoc["totalCapacity"].AsInt32);
        Assert.Equal(3, bsonDoc["availableCapacity"].AsInt32);
        Assert.Equal(20.0, bsonDoc["energyOfferedKwh"].AsDouble);
        Assert.True(bsonDoc["isActive"].AsBoolean);

        var deserialized = BsonSerializer.Deserialize<EnergyBookingSlot>(bsonDoc);
        Assert.Equal(slot.SlotId, deserialized.SlotId);
        Assert.Equal(slot.NodeId, deserialized.NodeId);
        Assert.Equal(slot.TotalCapacity, deserialized.TotalCapacity);
        Assert.Equal(slot.AvailableCapacity, deserialized.AvailableCapacity);
    }

    // Verifies that EnergyReservation default status is Pending and properties map correctly to BSON.
    [Fact]
    public void EnergyReservation_DefaultValues_And_BsonMapping()
    {
        var now = DateTime.UtcNow;
        var reservation = new EnergyReservation
        {
            ReservationId = "RS-0107",
            Nic = "199512345678",
            NodeId = "ND-003",
            SlotId = "SL-0042",
            SlotStartTime = now.AddDays(1),
            SlotEndTime = now.AddDays(1).AddHours(2),
            RequestedKwh = 15.0,
            CreatedAt = now,
            UpdatedAt = now
        };

        Assert.Equal("Pending", reservation.Status);
        Assert.Null(reservation.QrToken);
        Assert.Null(reservation.CancelledAt);
        Assert.Null(reservation.CompletedAt);

        var bsonDoc = reservation.ToBsonDocument();
        Assert.Equal("RS-0107", bsonDoc["reservationId"].AsString);
        Assert.Equal("199512345678", bsonDoc["nic"].AsString);
        Assert.Equal("ND-003", bsonDoc["nodeId"].AsString);
        Assert.Equal("SL-0042", bsonDoc["slotId"].AsString);
        Assert.Equal(15.0, bsonDoc["requestedKwh"].AsDouble);
        Assert.Equal("Pending", bsonDoc["status"].AsString);

        var deserialized = BsonSerializer.Deserialize<EnergyReservation>(bsonDoc);
        Assert.Equal(reservation.ReservationId, deserialized.ReservationId);
        Assert.Equal(reservation.Nic, deserialized.Nic);
        Assert.Equal(reservation.Status, deserialized.Status);
    }
}
