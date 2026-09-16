// ---------------------------------------------------------------------------
//  File        : RegisterProsumerRequest.cs
//  Project     : Smart Solar Microgrid Trading System - Web API
//  Module      : DTOs / Prosumers
//  Description : Request body for the public POST /api/prosumers/register
//                endpoint. No authentication required (REQ-PROS §4.1, UF-01).
//  Author      : <member name - M1>
//  Created     : 2026-09-16
// ---------------------------------------------------------------------------

namespace SmartSolar.Api.DTOs.Prosumers;

public record RegisterProsumerRequest(
    // NIC becomes the MongoDB _id (ADR-002). Both old and new Sri Lankan
    // formats accepted: ^([0-9]{9}[vVxX]|[0-9]{12})$ (REQ-PROS EC-1).
    string Nic,
    string FullName,
    string Email,
    string Phone,
    string Address,

    // Plain-text password — hashed before storage.
    string Password,

    // Declared solar array size in kilowatts.
    double SolarCapacityKw
);
