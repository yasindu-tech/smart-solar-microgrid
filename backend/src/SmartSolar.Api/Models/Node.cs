using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace SmartSolar.Api.Models;

public class Node
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
