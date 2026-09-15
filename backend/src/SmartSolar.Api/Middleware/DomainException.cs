// ---------------------------------------------------------------------------
//  File        : DomainException.cs
//  Project     : Smart Solar Microgrid Trading System - Web API
//  Module      : Middleware
//  Description : Exception type carrying a documented error code from
//                docs/architecture/api-contract.md section 9. Every business
//                rule violation is signalled with this type so that the error
//                middleware can translate it into the single error envelope
//                both clients understand.
//  Author      : <member name>
//  Created     : 2026-09-13
// ---------------------------------------------------------------------------

namespace SmartSolar.Api.Middleware;

/// <summary>Thrown by services when a business rule (BR-*) is violated.</summary>
public class DomainException : Exception
{
    public string Code { get; }
    public string? Field { get; }
    public int StatusCode { get; }

    // Construct a domain exception from a documented error code, the user-facing
    // message and the HTTP status the contract assigns to that code.
    public DomainException(string code, string message, int statusCode = 409, string? field = null)
        : base(message)
    {
        Code = code;
        Field = field;
        StatusCode = statusCode;
    }
}
