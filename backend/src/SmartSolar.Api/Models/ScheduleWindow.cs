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
