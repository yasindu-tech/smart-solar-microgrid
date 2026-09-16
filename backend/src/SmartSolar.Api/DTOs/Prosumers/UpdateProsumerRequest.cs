// ---------------------------------------------------------------------------
//  File        : UpdateProsumerRequest.cs
//  Project     : Smart Solar Microgrid Trading System - Web API
//  Module      : DTOs / Prosumers
//  Description : Request body for PUT /api/prosumers/{nic}. NIC is immutable
//                and must never appear here — the service ignores any nic
//                field in the body (BR-12, REQ-PROS AC-5).
//  Author      : <member name - M1>
//  Created     : 2026-09-16
// ---------------------------------------------------------------------------

namespace SmartSolar.Api.DTOs.Prosumers;

public record UpdateProsumerRequest(
    // All editable fields except NIC (BR-12 — NIC is immutable).
    string FullName,
    string Email,
    string Phone,
    string Address,
    double SolarCapacityKw
);
