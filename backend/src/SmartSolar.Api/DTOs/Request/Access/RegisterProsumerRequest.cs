namespace SmartSolar.Api.DTOs.Request.Access;

public record RegisterProsumerRequest(
    // NIC becomes the MongoDB _id (ADR-002). Both old and new Sri Lankan
    // formats accepted: ^([0-9]{9}[vVxX]|[0-9]{12})$ (REQ-PROS EC-1).
    string Nic,
    string FullName,
    string Email,
    string Phone,
    string Address,

    // Plain-text password — hashed before storage.
    string Password,

    // Declared solar array size in kilowatts.
    double SolarCapacityKw
);
