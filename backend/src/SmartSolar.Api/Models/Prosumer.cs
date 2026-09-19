using MongoDB.Bson.Serialization.Attributes;

namespace SmartSolar.Api.Models;

[BsonIgnoreExtraElements]
public class Prosumer
{
    // NIC IS the _id. Stored as a string, not ObjectId (ADR-002 §1).
    [BsonId]
    public string Id { get; set; } = string.Empty;

    // Duplicated for query clarity and serialisation comfort (ADR-002 §1).
    [BsonElement("nic")]
    public string Nic { get; set; } = string.Empty;

    // Full display name.
    [BsonElement("fullName")]
    public string FullName { get; set; } = string.Empty;

    // Email address.
    [BsonElement("email")]
    public string Email { get; set; } = string.Empty;

    // Phone number.
    [BsonElement("phone")]
    public string Phone { get; set; } = string.Empty;

    // Residential address.
    [BsonElement("address")]
    public string Address { get; set; } = string.Empty;

    // BCrypt password hash — never exposed in any response (NFR-2).
    [BsonElement("passwordHash")]
    public string PasswordHash { get; set; } = string.Empty;

    // Declared solar array capacity in kilowatts.
    [BsonElement("solarCapacityKw")]
    public double SolarCapacityKw { get; set; }

    // Account lifecycle status: "Active", "PendingActivation", "Deactivated".
    [BsonElement("status")]
    public string Status { get; set; } = "PendingActivation";

    // Timestamp when the prosumer requested deactivation. Null if never (BR-04).
    [BsonElement("deactivationRequestedAt")]
    [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
    public DateTime? DeactivationRequestedAt { get; set; }

    // UserId of the Backoffice officer who last reactivated this account (BR-03).
    [BsonElement("reactivatedBy")]
    public string? ReactivatedBy { get; set; }

    // Timestamp when the account was created (UTC — BR-13).
    [BsonElement("createdAt")]
    [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
    public DateTime CreatedAt { get; set; }

    // Timestamp of the last update (UTC — BR-13).
    [BsonElement("updatedAt")]
    [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
    public DateTime UpdatedAt { get; set; }
}
