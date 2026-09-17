namespace SmartSolar.Api.DTOs.Request.Access;

public record UpdateProsumerRequest(
    // All editable fields except NIC (BR-12 — NIC is immutable).
    string FullName,
    string Email,
    string Phone,
    string Address,
    double SolarCapacityKw
);
