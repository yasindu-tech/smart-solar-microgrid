// ---------------------------------------------------------------------------
//  File        : EnergyBookingSlot.cs
//  Project     : Smart Solar Microgrid Trading System - Web API
//  Description : Mongo document shape for the "EnergyBookingSlots" collection
//                (data-model.md 1.4) - a bookable time window at a node.
// ---------------------------------------------------------------------------

using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace SmartSolar.Api.Models;

public class EnergyBookingSlot
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? Id { get; set; }

    [BsonElement("slotId")]
    public string SlotId { get; set; } = string.Empty;

    [BsonElement("nodeId")]
    public string NodeId { get; set; } = string.Empty;

    [BsonElement("startTime")]
    public DateTime StartTime { get; set; }

    [BsonElement("endTime")]
    public DateTime EndTime { get; set; }

    [BsonElement("totalCapacity")]
    public int TotalCapacity { get; set; }

    [BsonElement("availableCapacity")]
    public int AvailableCapacity { get; set; }

    [BsonElement("energyOfferedKwh")]
    public double EnergyOfferedKwh { get; set; }

    [BsonElement("isActive")]
    public bool IsActive { get; set; } = true;

    [BsonElement("createdAt")]
    public DateTime CreatedAt { get; set; }

    [BsonElement("updatedAt")]
    public DateTime UpdatedAt { get; set; }
}
