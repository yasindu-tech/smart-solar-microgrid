// ---------------------------------------------------------------------------
//  File        : UserStatusRequest.cs
//  Project     : Smart Solar Microgrid Trading System - Web API
//  Module      : DTOs / Users
//  Description : Request body for PATCH /api/users/{userId}/status.
//                Used by Backoffice to activate or soft-deactivate a
//                web user account (REQ-USER §6 AC-4).
//  Author      : <member name - M1>
//  Created     : 2026-09-16
// ---------------------------------------------------------------------------

namespace SmartSolar.Api.DTOs.Users;

public record UserStatusRequest(
    // True = activate, false = deactivate (soft-delete).
    bool IsActive
);
