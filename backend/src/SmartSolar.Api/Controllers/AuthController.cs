// ---------------------------------------------------------------------------
//  File        : AuthController.cs
//  Project     : Smart Solar Microgrid Trading System - Web API
//  Module      : Controllers
//  Description : Handles POST /api/auth/login, POST /api/auth/logout, and
//                GET /api/auth/me. All domain logic is in AuthService —
//                this controller only binds, calls, and maps results.
//  Author      : <member name - M1>
//  Created     : 2026-09-16
// ---------------------------------------------------------------------------

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartSolar.Api.DTOs.Auth;
using SmartSolar.Api.Services;

namespace SmartSolar.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly AuthService _authService;

    // Inject the auth service.
    public AuthController(AuthService authService)
    {
        _authService = authService;
    }

    // POST /api/auth/login — open to unauthenticated callers.
    // Returns a signed JWT on success; E4301 or E4302 on failure.
    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var response = await _authService.LoginAsync(request.Identifier, request.Password);
        return Ok(response);
    }

    // POST /api/auth/logout — token is stateless so the server just returns 204.
    // The client must delete the stored token (web: sessionStorage, Android: SQLite).
    [HttpPost("logout")]
    [Authorize]
    public IActionResult Logout()
    {
        return NoContent();
    }

    // GET /api/auth/me — returns the identity embedded in the current token.
    // Useful for clients to refresh their stored profile without re-login.
    [HttpGet("me")]
    [Authorize]
    public IActionResult Me()
    {
        var principal = _authService.GetCurrentPrincipal(User);
        return Ok(principal);
    }
}
