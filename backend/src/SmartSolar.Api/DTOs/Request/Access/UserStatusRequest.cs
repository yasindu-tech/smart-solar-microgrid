namespace SmartSolar.Api.DTOs.Request.Access;

public record UserStatusRequest(
    // True = activate, false = deactivate (soft-delete).
    bool IsActive
);
