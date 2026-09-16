// ---------------------------------------------------------------------------
//  File        : UserResponse.cs
//  Project     : Smart Solar Microgrid Trading System - Web API
//  Module      : DTOs / Users
//  Description : Safe outbound representation of a User. Never includes
//                passwordHash (NFR-2, REQ-USER AC-8).
//  Author      : <member name - M1>
//  Created     : 2026-09-16
// ---------------------------------------------------------------------------

namespace SmartSolar.Api.DTOs.Users;

public record UserResponse(
    string UserId,
    string FullName,
    string Email,
    string Role,
    List<string> AssignedNodeIds,
    bool IsActive,
    DateTime CreatedAt,
    DateTime UpdatedAt
);
