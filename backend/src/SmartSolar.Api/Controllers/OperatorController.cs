// ---------------------------------------------------------------------------
//  File        : OperatorController.cs
//  Project     : Smart Solar Microgrid Trading System - Web API
//  Description : REST controller for grid operator QR verification and energy
//                transfer finalisation (DOM-04, REQ-QRV, api-contract §7).
//                Verify is read-only; finalize is the only endpoint that writes.
// ---------------------------------------------------------------------------

using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartSolar.Api.DTOs.Request.Operator;
using SmartSolar.Api.Services;

namespace SmartSolar.Api.Controllers;

[ApiController]
[Route("api/operator")]
public class OperatorController : ControllerBase
{
    private readonly OperatorVerificationService _verificationService;

    // Injects the operator verification business service.
    public OperatorController(OperatorVerificationService verificationService)
    {
        // Inject operator verification business service.
        _verificationService = verificationService;
    }

    /// <summary>
    /// Verifies a scanned QR token against the server and returns the booking details.
    /// Read-only — calling it twice leaves the database unchanged (REQ-QRV AC-9).
    /// </summary>
    [HttpPost("verify-qr")]
    [Authorize(Roles = "Operator,Backoffice")]
    public async Task<IActionResult> VerifyQr([FromBody] VerifyQrRequest request)
    {
        // Verify the scanned token server-side and return booking facts for operator confirmation.
        var callerUserId = GetCallerSubject();
        var callerRole = GetCallerRole();
        var response = await _verificationService.VerifyQrAsync(request, callerUserId, callerRole);
        return Ok(response);
    }

    /// <summary>
    /// Finalises a verified energy transfer, marking the reservation Completed and
    /// invalidating its QR token (BR-10, BR-11, BR-18).
    /// </summary>
    [HttpPost("finalize")]
    [Authorize(Roles = "Operator")]
    public async Task<IActionResult> Finalize([FromBody] FinalizeTransferRequest request)
    {
        // Re-run every verification check, then commit the completed energy transfer.
        var callerUserId = GetCallerSubject();
        var callerRole = GetCallerRole();
        var response = await _verificationService.FinalizeAsync(request, callerUserId, callerRole);
        return Ok(response);
    }

    // Helper extracting the primary identity subject claim from current principal.
    private string GetCallerSubject()
    {
        // Extract identity subject claim from caller claims principal.
        return User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.FindFirstValue("sub")
            ?? string.Empty;
    }

    // Helper extracting the role claim from current principal.
    private string GetCallerRole()
    {
        // Extract role claim from caller claims principal.
        return User.FindFirstValue(ClaimTypes.Role) ?? string.Empty;
    }
}
