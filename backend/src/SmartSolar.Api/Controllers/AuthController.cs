using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartSolar.Api.DTOs.Request.Access;
using SmartSolar.Api.DTOs.Response.Access;
using SmartSolar.Api.DTOs.Response.Common;
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

    /// <summary>
    /// Authenticates a user and returns a JWT token.
    /// </summary>
    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var response = await _authService.LoginAsync(request.Identifier, request.Password);
        return Ok(response);
    }

    /// <summary>
    /// Logs out a user.
    /// </summary>
    [HttpPost("logout")]
    [Authorize]
    public IActionResult Logout()
    {
        return NoContent();
    }

    /// <summary>
    /// Gets the current authenticated user's profile information.
    /// </summary>
    [HttpGet("me")]
    [Authorize]
    public IActionResult Me()
    {
        var principal = _authService.GetCurrentPrincipal(User);
        return Ok(principal);
    }
}
