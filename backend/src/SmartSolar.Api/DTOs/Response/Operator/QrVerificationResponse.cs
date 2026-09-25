// ---------------------------------------------------------------------------
//  File        : QrVerificationResponse.cs
//  Project     : Smart Solar Microgrid Trading System - Web API
//  Description : Response DTO returned by the read-only QR verification call,
//                giving the operator the booking facts to confirm (REQ-QRV AC-2).
// ---------------------------------------------------------------------------

namespace SmartSolar.Api.DTOs.Response.Operator;

public record QrVerificationResponse(
    bool Valid,
    string ReservationId,
    string ProsumerName,
    string Nic,
    string NodeId,
    string NodeName,
    DateTime SlotStartTime,
    DateTime SlotEndTime,
    double RequestedKwh,
    string Status
);
