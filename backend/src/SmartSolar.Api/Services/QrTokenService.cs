// ---------------------------------------------------------------------------
//  File        : QrTokenService.cs
//  Project     : Smart Solar Microgrid Trading System - Web API
//  Description : Cryptographic token service generating and verifying signed QR
//                payload tokens for approved reservations per ADR-003. Verification
//                is server-side only (BR-10) - no client ever parses a payload.
// ---------------------------------------------------------------------------

using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using System.Text.Json.Serialization;
using SmartSolar.Api.Configuration;
using SmartSolar.Api.Middleware;

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

    // Verifies the presented token's structure and HMAC-SHA256 signature (BR-10, ADR-003 §3).
    // Throws E4201 on anything malformed, mis-signed, or unparseable - never a raw crash.
    public QrTokenPayload ValidateToken(string? presentedToken)
    {
        // Reject an absent or blank token before any cryptographic work.
        if (string.IsNullOrWhiteSpace(presentedToken))
        {
            throw new DomainException("E4201", "QR code is malformed or was not signed by this system.", 400, "qrToken");
        }

        var token = presentedToken.Trim();

        // ADR-003 §2: the token is exactly "<base64url payload>.<base64url signature>".
        var separatorIndex = token.IndexOf('.');
        if (separatorIndex <= 0 || separatorIndex != token.LastIndexOf('.') || separatorIndex == token.Length - 1)
        {
            throw new DomainException("E4201", "QR code is malformed or was not signed by this system.", 400, "qrToken");
        }

        var payloadEncoded = token[..separatorIndex];
        var signatureEncoded = token[(separatorIndex + 1)..];

        byte[] presentedSignature;
        byte[] payloadBytes;
        try
        {
            presentedSignature = Base64UrlDecode(signatureEncoded);
            payloadBytes = Base64UrlDecode(payloadEncoded);
        }
        catch (FormatException)
        {
            throw new DomainException("E4201", "QR code is malformed or was not signed by this system.", 400, "qrToken");
        }

        // Recompute the signature over the presented payload and compare in constant time.
        using var hmac = new HMACSHA256(_keyBytes);
        var expectedSignature = hmac.ComputeHash(Encoding.UTF8.GetBytes(payloadEncoded));

        if (presentedSignature.Length != expectedSignature.Length
            || !CryptographicOperations.FixedTimeEquals(presentedSignature, expectedSignature))
        {
            throw new DomainException("E4201", "QR code is malformed or was not signed by this system.", 400, "qrToken");
        }

        // Signature is ours, so the payload is safe to deserialize.
        QrTokenPayload? payload;
        try
        {
            payload = JsonSerializer.Deserialize<QrTokenPayload>(payloadBytes);
        }
        catch (JsonException)
        {
            throw new DomainException("E4201", "QR code is malformed or was not signed by this system.", 400, "qrToken");
        }

        if (payload == null || string.IsNullOrWhiteSpace(payload.R) || string.IsNullOrWhiteSpace(payload.N))
        {
            throw new DomainException("E4201", "QR code is malformed or was not signed by this system.", 400, "qrToken");
        }

        return payload;
    }

    // Decodes a URL-safe Base64 string, restoring the padding stripped at encode time.
    private static byte[] Base64UrlDecode(string input)
    {
        // Restore standard Base64 alphabet and pad to a multiple of four characters.
        var standard = input.Replace('-', '+').Replace('_', '/');
        var padding = standard.Length % 4;
        if (padding == 1)
        {
            throw new FormatException("Invalid Base64Url length.");
        }
        if (padding > 0)
        {
            standard = standard.PadRight(standard.Length + (4 - padding), '=');
        }

        return Convert.FromBase64String(standard);
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

/// <summary>
/// Decoded QR token payload as defined in ADR-003 §2. Field names are deliberately
/// single characters to keep the QR low-density and reliable on a cheap camera.
/// </summary>
public record QrTokenPayload
{
    // Reservation identifier the token was issued for, e.g. "RS-0107".
    [JsonPropertyName("r")]
    public string R { get; init; } = string.Empty;

    // Random 16-byte nonce, regenerated on every issue so an old QR stops matching.
    [JsonPropertyName("n")]
    public string N { get; init; } = string.Empty;

    // Unix timestamp (seconds) recording when the token was issued.
    [JsonPropertyName("t")]
    public long T { get; init; }
}
