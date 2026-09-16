// ---------------------------------------------------------------------------
//  File        : PrincipalResponse.cs
//  Project     : Smart Solar Microgrid Trading System - Web API
//  Module      : DTOs / Auth
//  Description : Response body for GET /api/auth/me. Returns the identity
//                claims from the validated JWT so clients can refresh their
//                stored profile without a full re-login.
//  Author      : <member name - M1>
//  Created     : 2026-09-16
// ---------------------------------------------------------------------------

namespace SmartSolar.Api.DTOs.Auth;

public record PrincipalResponse(
    // UserId or NIC — whatever is in the "sub" claim of the token.
    string SubjectId,

    // "Backoffice", "Operator", or "Prosumer".
    string Role,

    // Display name from the "name" claim.
    string DisplayName
);
