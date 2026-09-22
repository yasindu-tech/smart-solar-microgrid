namespace SmartSolar.Api.DTOs.Request.Slots;

/// <summary>PATCH /api/slots/{slotId}/availability body - the operator's daily capacity update.</summary>
public record UpdateSlotAvailabilityRequest(int AvailableCapacity);
