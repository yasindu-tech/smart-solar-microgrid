// ---------------------------------------------------------------------------
//  File        : CreateReservationRequest.cs
//  Project     : Smart Solar Microgrid Trading System - Web API
//  Description : Request payload for creating a new energy transfer reservation.
// ---------------------------------------------------------------------------

using System.ComponentModel.DataAnnotations;

namespace SmartSolar.Api.DTOs.Request.Reservations;

public class CreateReservationRequest
{
    // Target microgrid node identifier, e.g. "ND-001".
    [Required]
    public string NodeId { get; set; } = string.Empty;

    // Bookable time window slot identifier, e.g. "SL-0001".
    [Required]
    public string SlotId { get; set; } = string.Empty;

    // Requested energy quantity in kilowatt-hours.
    [Range(0.01, 1000.0, ErrorMessage = "Requested kWh must be greater than 0.")]
    public double RequestedKwh { get; set; }

    // Optional prosumer NIC when created on behalf by staff (Backoffice or Operator).
    public string? Nic { get; set; }
}

