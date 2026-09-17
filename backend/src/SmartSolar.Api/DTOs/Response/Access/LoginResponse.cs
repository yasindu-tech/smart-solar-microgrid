namespace SmartSolar.Api.DTOs.Response.Access;

public record LoginResponse(
    // Signed JWT for subsequent Bearer-authenticated requests.
    string Token,

    // "Backoffice", "Operator", or "Prosumer" — used by clients to route home.
    string Role,

    // Display name for the welcome header.
    string DisplayName,

    // UserId for web users; NIC for prosumers.
    [property: System.Text.Json.Serialization.JsonPropertyName("nicOrUserId")]
    string SubjectId,

    // UTC expiry so clients can schedule pre-emptive logout.
    DateTime ExpiresAt
);
