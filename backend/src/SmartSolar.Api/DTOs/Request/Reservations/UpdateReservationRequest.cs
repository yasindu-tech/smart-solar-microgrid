// ---------------------------------------------------------------------------
//  File        : UpdateReservationRequest.cs
//  Project     : Smart Solar Microgrid Trading System - Web API
//  Description : Request payload for modifying a reservation slot or kWh amount.
// ---------------------------------------------------------------------------

using System.ComponentModel.DataAnnotations;

namespace SmartSolar.Api.DTOs.Request.Reservations;

public class UpdateReservationRequest
{
    // Updated slot identifier to reschedule to, e.g. "SL-0002".
    [Required]
    public string SlotId { get; set; } = string.Empty;

    // Updated energy quantity in kilowatt-hours.
    [Range(0.01, 1000.0, ErrorMessage = "Requested kWh must be greater than 0.")]
    public double RequestedKwh { get; set; }
}
