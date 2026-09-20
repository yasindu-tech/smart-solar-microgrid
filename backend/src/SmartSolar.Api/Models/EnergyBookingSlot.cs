// ---------------------------------------------------------------------------
//  File        : EnergyBookingSlot.cs
//  Project     : Smart Solar Microgrid Trading System - Web API
//  Description : Mongo document model representing a bookable energy transfer
//                time window at a microgrid node ("EnergyBookingSlots" collection).
// ---------------------------------------------------------------------------

using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace SmartSolar.Api.Models;

[BsonIgnoreExtraElements]
public class EnergyBookingSlot
{
    // MongoDB auto-generated document identifier.
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? Id { get; set; }

    // Unique human-readable slot identifier, e.g. "SL-0042".
    [BsonElement("slotId")]
    public string SlotId { get; set; } = string.Empty;

    // Target microgrid node identifier (reference -> SolarStationInfo.nodeId).
    [BsonElement("nodeId")]
    public string NodeId { get; set; } = string.Empty;

    // Start time of the energy booking slot in UTC (BR-13).
    [BsonElement("startTime")]
    [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
    public DateTime StartTime { get; set; }

    // End time of the energy booking slot in UTC (BR-13).
    [BsonElement("endTime")]
    [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
    public DateTime EndTime { get; set; }

    // Total number of reservations this slot can hold simultaneously.
    [BsonElement("totalCapacity")]
    public int TotalCapacity { get; set; }

    // Remaining capacity available for booking. Decremented on approval, restored on cancel (BR-15).
    [BsonElement("availableCapacity")]
    public int AvailableCapacity { get; set; }

    // Total energy available or offered during this slot window in kilowatt-hours.
    [BsonElement("energyOfferedKwh")]
    public double EnergyOfferedKwh { get; set; }

    // Flag indicating whether this slot is currently active and bookable.
    [BsonElement("isActive")]
    public bool IsActive { get; set; } = true;

    // Timestamp when the booking slot was created (UTC — BR-13).
    [BsonElement("createdAt")]
    [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
    public DateTime CreatedAt { get; set; }

    // Timestamp of the last update to the booking slot (UTC — BR-13).
    [BsonElement("updatedAt")]
    [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
    public DateTime UpdatedAt { get; set; }
}
