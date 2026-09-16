// ---------------------------------------------------------------------------
//  File        : AuthService.cs
//  Project     : Smart Solar Microgrid Trading System - Web API
//  Module      : Services
//  Description : Authentication business logic. Handles login for all three
//                roles using a dual-collection lookup (REQ-AUTH §4 step 2):
//                first tries Users by email, then Prosumers by NIC.
//                Issues JWTs via JwtTokenService.
//  Author      : <member name - M1>
//  Created     : 2026-09-16
// ---------------------------------------------------------------------------

using System.Security.Claims;
using SmartSolar.Api.DTOs.Auth;
using SmartSolar.Api.Middleware;
using SmartSolar.Api.Repositories;

namespace SmartSolar.Api.Services;

public class AuthService
{
    private readonly UserRepository _userRepo;
    private readonly ProsumerRepository _prosumerRepo;
    private readonly JwtTokenService _jwtService;

    // Inject repositories and the JWT generator.
    public AuthService(
        UserRepository userRepo,
        ProsumerRepository prosumerRepo,
        JwtTokenService jwtService)
    {
        _userRepo = userRepo;
        _prosumerRepo = prosumerRepo;
        _jwtService = jwtService;
    }

    // Authenticate a login attempt. Returns a JWT on success.
    // Error codes per api-contract.md §9:
    //   E4301 — invalid credentials (used for both "not found" and "wrong password"
    //           to avoid revealing whether the account exists — REQ-AUTH AC-5).
    //   E4302 — account inactive (deactivated or pending approval — AC-6, AC-7).
    public async Task<LoginResponse> LoginAsync(string identifier, string password)
    {
        // Step 1: try to find a web user by email (Backoffice or Operator).
        var webUser = await _userRepo.GetByEmailAsync(identifier.Trim());
        if (webUser != null)
        {
            // Verify the BCrypt hash — wrong password returns generic E4301.
            if (!BCrypt.Net.BCrypt.Verify(password, webUser.PasswordHash))
                throw new DomainException("E4301", "Invalid credentials.", 401);

            // Account must be active (BR-17).
            if (!webUser.IsActive)
                throw new DomainException("E4302",
                    "Your account has been deactivated. Contact Backoffice to reactivate.", 403);

            var (token, expiresAt) = _jwtService.GenerateToken(
                webUser.UserId, webUser.Role, webUser.FullName);

            return new LoginResponse(token, webUser.Role, webUser.FullName, webUser.UserId, expiresAt);
        }

        // Step 2: try to find a prosumer by NIC (case-insensitive, trimmed — REQ-AUTH EC-1).
        var prosumer = await _prosumerRepo.GetByNicAsync(identifier.Trim());
        if (prosumer != null)
        {
            // Verify hash.
            if (!BCrypt.Net.BCrypt.Verify(password, prosumer.PasswordHash))
                throw new DomainException("E4301", "Invalid credentials.", 401);

            // Prosumer must be Active — PendingActivation and Deactivated both return E4302.
            if (prosumer.Status == "PendingActivation")
                throw new DomainException("E4302",
                    "Your account is awaiting Backoffice approval.", 403);

            if (prosumer.Status == "Deactivated")
                throw new DomainException("E4302",
                    "Your account has been deactivated. Contact Backoffice to reactivate.", 403);

            var (token, expiresAt) = _jwtService.GenerateToken(
                prosumer.Nic, "Prosumer", prosumer.FullName);

            return new LoginResponse(token, "Prosumer", prosumer.FullName, prosumer.Nic, expiresAt);
        }

        // Neither collection contained a matching account — generic message (REQ-AUTH AC-5).
        throw new DomainException("E4301", "Invalid credentials.", 401);
    }

    // Extract the current principal's identity from the validated JWT claims.
    public PrincipalResponse GetCurrentPrincipal(ClaimsPrincipal user)
    {
        var subjectId = user.FindFirstValue(ClaimTypes.NameIdentifier)
                     ?? user.FindFirstValue("sub")
                     ?? string.Empty;
        var role = user.FindFirstValue(ClaimTypes.Role) ?? string.Empty;
        var displayName = user.FindFirstValue("name") ?? string.Empty;

        return new PrincipalResponse(subjectId, role, displayName);
    }
}
