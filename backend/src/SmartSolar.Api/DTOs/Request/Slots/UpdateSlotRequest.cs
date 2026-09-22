namespace SmartSolar.Api.DTOs.Request.Slots;

/// <summary>PUT /api/slots/{slotId} body. Re-validated exactly like a create (REQ-SLOT 4.3).</summary>
public record UpdateSlotRequest(
    DateTime StartTime,
    DateTime EndTime,
    int TotalCapacity,
    double EnergyOfferedKwh);
