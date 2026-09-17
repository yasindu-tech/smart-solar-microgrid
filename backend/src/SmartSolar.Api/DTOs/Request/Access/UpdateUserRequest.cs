namespace SmartSolar.Api.DTOs.Request.Access;

public record UpdateUserRequest(
    string FullName,
    string Email,

    // Null means "do not change the password".
    string? Password,

    // Must be "Backoffice" or "Operator".
    string Role,

    // Required for Operators; cleared when role is Backoffice (REQ-USER AC-7).
    List<string>? AssignedNodeIds
);
