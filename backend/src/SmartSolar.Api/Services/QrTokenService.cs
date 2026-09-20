// ---------------------------------------------------------------------------
//  File        : QrTokenService.cs
//  Project     : Smart Solar Microgrid Trading System - Web API
//  Description : Cryptographic token service generating signed QR payload tokens
//                for approved reservations per ADR-003.
// ---------------------------------------------------------------------------

using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using SmartSolar.Api.Configuration;

namespace SmartSolar.Api.Services;

public class QrTokenService
{
    private readonly byte[] _keyBytes;

    // Initializes the QR token service with the configured signing secret.
    public QrTokenService(IOptions<QrSettings> qrSettings)
    {
        // Resolve secret key bytes from configuration.
        var secret = qrSettings.Value.SigningSecret?.Trim();
        if (string.IsNullOrWhiteSpace(secret))
        {
            throw new InvalidOperationException("QR signing secret must be configured.");
        }
        _keyBytes = Encoding.UTF8.GetBytes(secret);
    }

    // Generates a signed QR token string compliant with ADR-003.
    public string GenerateToken(string reservationId)
    {
        // Generate a cryptographically secure 16-byte random nonce.
        var nonceBytes = new byte[16];
        RandomNumberGenerator.Fill(nonceBytes);
        var nonce = Base64UrlEncode(nonceBytes);

        var issuedAt = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

        var payloadObj = new
        {
            r = reservationId,
            n = nonce,
            t = issuedAt
        };

        var json = JsonSerializer.Serialize(payloadObj);
        var payloadEncoded = Base64UrlEncode(Encoding.UTF8.GetBytes(json));

        // Compute HMAC-SHA256 signature over the payload string.
        using var hmac = new HMACSHA256(_keyBytes);
        var signatureBytes = hmac.ComputeHash(Encoding.UTF8.GetBytes(payloadEncoded));
        var signatureEncoded = Base64UrlEncode(signatureBytes);

        return $"{payloadEncoded}.{signatureEncoded}";
    }

    // Encodes raw bytes into URL-safe Base64 string without padding.
    private static string Base64UrlEncode(byte[] input)
    {
        // Convert to Base64 and trim standard padding and URL-unsafe chars.
        return Convert.ToBase64String(input)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
    }
}
