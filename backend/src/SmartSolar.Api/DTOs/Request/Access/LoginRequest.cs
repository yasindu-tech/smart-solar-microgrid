namespace SmartSolar.Api.DTOs.Request.Access;

public record LoginRequest(
    // Email for Backoffice/Operator accounts; NIC for Prosumer accounts.
    string Identifier,
    string Password
);
