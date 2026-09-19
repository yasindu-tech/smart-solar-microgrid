// ---------------------------------------------------------------------------
//  File        : SolarStationInfo.cs
//  Project     : Smart Solar Microgrid Trading System - Web API
//  Description : Mongo document shapes for the "SolarStationInfo" collection
//                (a microgrid node): the schedule window, the GPS location,
//                and the node itself.
// ---------------------------------------------------------------------------

using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace SmartSolar.Api.Models;

public class ScheduleWindow
{
    [BsonElement("dayOfWeek")]
    public string DayOfWeek { get; set; } = string.Empty;

    [BsonElement("openTime")]
    public string OpenTime { get; set; } = string.Empty;

    [BsonElement("closeTime")]
    public string CloseTime { get; set; } = string.Empty;
}

public class NodeLocation
{
    [BsonElement("latitude")]
    public double Latitude { get; set; }

    [BsonElement("longitude")]
    public double Longitude { get; set; }

    [BsonElement("addressLine")]
    public string AddressLine { get; set; } = string.Empty;
}

public class SolarStationInfo
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? Id { get; set; }

    [BsonElement("nodeId")]
    public string NodeId { get; set; } = string.Empty;

    [BsonElement("name")]
    public string Name { get; set; } = string.Empty;

    [BsonElement("location")]
    public NodeLocation Location { get; set; } = new();

    [BsonElement("capacityKwh")]
    public double CapacityKwh { get; set; }

    [BsonElement("batterySlotCount")]
    public int BatterySlotCount { get; set; }

    [BsonElement("type")]
    public string Type { get; set; } = "AC";

    [BsonElement("schedule")]
    public List<ScheduleWindow> Schedule { get; set; } = new();

    [BsonElement("isActive")]
    public bool IsActive { get; set; } = true;

    [BsonElement("createdBy")]
    public string CreatedBy { get; set; } = string.Empty;

    [BsonElement("createdAt")]
    public DateTime CreatedAt { get; set; }

    [BsonElement("updatedAt")]
    public DateTime UpdatedAt { get; set; }
}
