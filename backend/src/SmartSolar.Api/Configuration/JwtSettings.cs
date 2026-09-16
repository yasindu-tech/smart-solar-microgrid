// ---------------------------------------------------------------------------
//  File        : JwtSettings.cs
//  Project     : Smart Solar Microgrid Trading System - Web API
//  Module      : Configuration
//  Description : Strongly typed JWT settings bound from the "Jwt" section of
//                appsettings.json. The signing key is supplied via
//                appsettings.Development.json (local) or
//                appsettings.Production.json (VM) — never committed.
//  Author      : <member name - M1>
//  Created     : 2026-09-16
// ---------------------------------------------------------------------------

namespace SmartSolar.Api.Configuration;

public class JwtSettings
{
    // HMAC-SHA256 signing key — must be at least 32 chars in production.
    public string SigningKey { get; set; } = string.Empty;

    // Issuer claim written into every token.
    public string Issuer { get; set; } = "SmartSolar.Api";

    // Audience claim written into every token.
    public string Audience { get; set; } = "SmartSolar.Clients";

    // Token lifetime in hours. REQ-AUTH §4 step 5 specifies 8 hours.
    public int ExpiryHours { get; set; } = 8;
}
