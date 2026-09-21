// ---------------------------------------------------------------------------
//  File        : QrSettings.cs
//  Project     : Smart Solar Microgrid Trading System - Web API
//  Description : Strongly-typed configuration options for QR code token signing
//                and verification (ADR-003).
// ---------------------------------------------------------------------------

namespace SmartSolar.Api.Configuration;

public class QrSettings
{
    // Secret key used to sign and verify QR code tokens via HMAC-SHA256.
    public string SigningSecret { get; set; } = string.Empty;
}
