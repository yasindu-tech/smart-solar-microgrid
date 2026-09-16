// ---------------------------------------------------------------------------
//  File        : ProsumerResponse.cs
//  Project     : Smart Solar Microgrid Trading System - Web API
//  Module      : DTOs / Prosumers
//  Description : Safe outbound representation of a Prosumer. Never includes
//                passwordHash (NFR-2).
//  Author      : <member name - M1>
//  Created     : 2026-09-16
// ---------------------------------------------------------------------------

namespace SmartSolar.Api.DTOs.Prosumers;

public record ProsumerResponse(
    string Nic,
    string FullName,
    string Email,
    string Phone,
    string Address,
    double SolarCapacityKw,
    string Status,
    DateTime? DeactivationRequestedAt,
    string? ReactivatedBy,
    DateTime CreatedAt,
    DateTime UpdatedAt
);
