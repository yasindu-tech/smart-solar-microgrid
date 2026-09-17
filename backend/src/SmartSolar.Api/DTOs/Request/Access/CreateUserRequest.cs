namespace SmartSolar.Api.DTOs.Request.Access;

public record CreateUserRequest(
    string FullName,
    string Email,

    // Plain-text password — hashed by the service before storage (≥ 8 chars).
    string Password,

    // Must be "Backoffice" or "Operator".
    string Role,

    // Required for Operators; ignored (cleared) for Backoffice.
    List<string>? AssignedNodeIds
);
