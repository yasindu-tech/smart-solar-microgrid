// ---------------------------------------------------------------------------
//  File        : LoginResponse.cs
//  Project     : Smart Solar Microgrid Trading System - Web API
//  Module      : DTOs / Auth
//  Description : Response body for a successful POST /api/auth/login.
//                Carries the signed JWT and enough profile data for the client
//                to route to the correct home screen without an extra round trip.
//  Author      : <member name - M1>
//  Created     : 2026-09-16
// ---------------------------------------------------------------------------

namespace SmartSolar.Api.DTOs.Auth;

public record LoginResponse(
    // Signed JWT for subsequent Bearer-authenticated requests.
    string Token,

    // "Backoffice", "Operator", or "Prosumer" — used by clients to route home.
    string Role,

    // Display name for the welcome header.
    string DisplayName,

    // UserId for web users; NIC for prosumers.
    string SubjectId,

    // UTC expiry so clients can schedule pre-emptive logout.
    DateTime ExpiresAt
);
