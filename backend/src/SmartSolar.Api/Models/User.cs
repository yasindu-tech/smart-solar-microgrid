using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace SmartSolar.Api.Models;

[BsonIgnoreExtraElements]
public class User
{
    // MongoDB auto-generated document identifier.
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? Id { get; set; }

    // Human-readable unique identifier, e.g. "USR-0001".
    [BsonElement("userId")]
    public string UserId { get; set; } = string.Empty;

    // Full display name.
    [BsonElement("fullName")]
    public string FullName { get; set; } = string.Empty;

    // Email — unique, used as the login identifier for web users.
    [BsonElement("email")]
    public string Email { get; set; } = string.Empty;

    // BCrypt password hash — never exposed in any response (NFR-2).
    [BsonElement("passwordHash")]
    public string PasswordHash { get; set; } = string.Empty;

    // Account role: "Backoffice" or "Operator".
    [BsonElement("role")]
    public string Role { get; set; } = string.Empty;

    // Node IDs the operator is assigned to. Always empty for Backoffice.
    [BsonElement("assignedNodeIds")]
    public List<string> AssignedNodeIds { get; set; } = new();

    // Whether the account is active. Soft-delete sets this false.
    [BsonElement("isActive")]
    public bool IsActive { get; set; } = true;

    // Timestamp when the account was created (UTC — BR-13).
    [BsonElement("createdAt")]
    [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
    public DateTime CreatedAt { get; set; }

    // Timestamp of the last update (UTC — BR-13).
    [BsonElement("updatedAt")]
    [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
    public DateTime UpdatedAt { get; set; }
}
