// ---------------------------------------------------------------------------
//  File        : VerifyQrRequest.cs
//  Project     : Smart Solar Microgrid Trading System - Web API
//  Description : Request DTO carrying the raw scanned QR token string from the
//                operator's mobile scanner (REQ-QRV §4, ADR-003 §3).
// ---------------------------------------------------------------------------

using System.ComponentModel.DataAnnotations;

namespace SmartSolar.Api.DTOs.Request.Operator;

public record VerifyQrRequest(
    // The scanned token exactly as read from the QR image. The device never parses it.
    string? QrToken
);
