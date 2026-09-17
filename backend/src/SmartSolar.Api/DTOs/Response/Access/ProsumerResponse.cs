namespace SmartSolar.Api.DTOs.Response.Access;

public record ProsumerResponse(
    string Nic,
    string FullName,
    string Email,
    string Phone,
    string Address,
    double SolarCapacityKw,
    string Status,
    DateTime? DeactivationRequestedAt,
    string? ReactivatedBy,
    DateTime CreatedAt,
    DateTime UpdatedAt
);
