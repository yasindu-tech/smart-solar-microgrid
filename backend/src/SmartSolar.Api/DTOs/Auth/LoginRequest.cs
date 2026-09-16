// ---------------------------------------------------------------------------
//  File        : LoginRequest.cs
//  Project     : Smart Solar Microgrid Trading System - Web API
//  Module      : DTOs / Auth
//  Description : Request body for POST /api/auth/login. The single Identifier
//                field covers both email (web users) and NIC (prosumers) —
//                the service resolves which collection to query (REQ-AUTH §4).
//  Author      : <member name - M1>
//  Created     : 2026-09-16
// ---------------------------------------------------------------------------

namespace SmartSolar.Api.DTOs.Auth;

public record LoginRequest(
    // Email for Backoffice/Operator accounts; NIC for Prosumer accounts.
    string Identifier,
    string Password
);
