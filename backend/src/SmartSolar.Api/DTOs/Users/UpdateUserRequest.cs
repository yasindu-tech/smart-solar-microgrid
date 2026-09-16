// ---------------------------------------------------------------------------
//  File        : UpdateUserRequest.cs
//  Project     : Smart Solar Microgrid Trading System - Web API
//  Module      : DTOs / Users
//  Description : Request body for PUT /api/users/{userId}. All fields except
//                userId are updatable. Password is optional — omit to keep
//                the current hash.
//  Author      : <member name - M1>
//  Created     : 2026-09-16
// ---------------------------------------------------------------------------

namespace SmartSolar.Api.DTOs.Users;

public record UpdateUserRequest(
    string FullName,
    string Email,

    // Null means "do not change the password".
    string? Password,

    // Must be "Backoffice" or "Operator".
    string Role,

    // Required for Operators; cleared when role is Backoffice (REQ-USER AC-7).
    List<string>? AssignedNodeIds
);
