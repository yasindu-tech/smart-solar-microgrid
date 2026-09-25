// ---------------------------------------------------------------------------
//  File        : FinalizeTransferRequest.cs
//  Project     : Smart Solar Microgrid Trading System - Web API
//  Description : Request DTO the operator submits to finalise a verified energy
//                transfer, carrying the metered delivered kWh (REQ-QRV §4 step 6).
// ---------------------------------------------------------------------------

using System.ComponentModel.DataAnnotations;

namespace SmartSolar.Api.DTOs.Request.Operator;

public record FinalizeTransferRequest(
    // The scanned token, re-presented so finalize can re-run every verification check (REQ-QRV EC-5).
    [Required(ErrorMessage = "A scanned QR token is required.")]
    string QrToken,

    // Reservation the operator confirmed on the verification card.
    [Required(ErrorMessage = "A reservation identifier is required.")]
    string ReservationId,

    // Energy actually delivered at the node, in kilowatt-hours. May exceed requestedKwh (REQ-QRV EC-4).
    [Range(0.0, double.MaxValue, ErrorMessage = "Delivered kWh cannot be negative.")]
    double DeliveredKwh
);
