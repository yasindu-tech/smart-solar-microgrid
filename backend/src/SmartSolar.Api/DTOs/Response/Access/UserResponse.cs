namespace SmartSolar.Api.DTOs.Response.Access;

public record UserResponse(
    string UserId,
    string FullName,
    string Email,
    string Role,
    List<string> AssignedNodeIds,
    bool IsActive,
    DateTime CreatedAt,
    DateTime UpdatedAt
);
