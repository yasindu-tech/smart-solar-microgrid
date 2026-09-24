// ---------------------------------------------------------------------------
//  File        : CreateReservationOnBehalfRequest.cs
//  Project     : Smart Solar Microgrid Trading System - Web API
//  Description : Request payload for staff creating a reservation on behalf
//                of a prosumer (DOM-03).
// ---------------------------------------------------------------------------

using System.ComponentModel.DataAnnotations;

namespace SmartSolar.Api.DTOs.Request.Reservations;

public class CreateReservationOnBehalfRequest : CreateReservationRequest
{
    // National Identity Card of the prosumer.
    [Required(ErrorMessage = "Prosumer NIC is required.")]
    public new string Nic { get; set; } = string.Empty;
}

