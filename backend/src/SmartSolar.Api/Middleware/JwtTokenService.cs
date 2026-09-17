using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using SmartSolar.Api.Configuration;

namespace SmartSolar.Api.Middleware;

public class JwtTokenService
{
    private readonly JwtSettings _settings;

    // Inject JwtSettings bound from the "Jwt" config section.
    public JwtTokenService(IOptions<JwtSettings> settings)
    {
        _settings = settings.Value;
    }

    // Generate a signed JWT for the given principal. The role claim is used
    // by the [Authorize(Roles = "...")] attributes on every protected endpoint.
    public (string Token, DateTime ExpiresAt) GenerateToken(
        string subjectId, string role, string displayName)
    {
        // Build the signing credentials from the configured key.
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_settings.SigningKey));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        // Expiry calculated using server clock only (BR-13).
        var expiresAt = DateTime.UtcNow.AddHours(_settings.ExpiryHours);

        // Standard claims plus the role and display-name custom claims.
        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, subjectId),
            new Claim(ClaimTypes.Role, role),
            new Claim(JwtRegisteredClaimNames.Name, displayName),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
        };

        var token = new JwtSecurityToken(
            issuer: _settings.Issuer,
            audience: _settings.Audience,
            claims: claims,
            expires: expiresAt,
            signingCredentials: creds);

        return (new JwtSecurityTokenHandler().WriteToken(token), expiresAt);
    }
}
