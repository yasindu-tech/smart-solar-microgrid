using MongoDB.Bson.Serialization.Attributes;

namespace SmartSolar.Api.Models;

public class NodeLocation
{
    [BsonElement("latitude")]
    public double Latitude { get; set; }

    [BsonElement("longitude")]
    public double Longitude { get; set; }

    [BsonElement("addressLine")]
    public string AddressLine { get; set; } = string.Empty;
}
