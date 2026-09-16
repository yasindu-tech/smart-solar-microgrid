// ---------------------------------------------------------------------------
//  File        : CreateUserRequest.cs
//  Project     : Smart Solar Microgrid Trading System - Web API
//  Module      : DTOs / Users
//  Description : Request body for POST /api/users. Backoffice-only endpoint
//                that creates a new Backoffice or Operator web account
//                (REQ-USER §4).
//  Author      : <member name - M1>
//  Created     : 2026-09-16
// ---------------------------------------------------------------------------

namespace SmartSolar.Api.DTOs.Users;

public record CreateUserRequest(
    string FullName,
    string Email,

    // Plain-text password — hashed by the service before storage (≥ 8 chars).
    string Password,

    // Must be "Backoffice" or "Operator".
    string Role,

    // Required for Operators; ignored (cleared) for Backoffice.
    List<string>? AssignedNodeIds
);
