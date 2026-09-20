// ---------------------------------------------------------------------------
//  File        : QrTokenResponse.cs
//  Project     : Smart Solar Microgrid Trading System - Web API
//  Description : Response DTO containing the signed QR token and slot expiration.
// ---------------------------------------------------------------------------

namespace SmartSolar.Api.DTOs.Response.Reservations;

public record QrTokenResponse(
    string QrToken,
    DateTime ExpiresAt
);
