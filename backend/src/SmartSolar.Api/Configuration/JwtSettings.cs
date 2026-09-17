namespace SmartSolar.Api.Configuration;

public class JwtSettings
{
    // HMAC-SHA256 signing key — must be at least 32 chars in production.
    public string SigningKey { get; set; } = string.Empty;

    // Issuer claim written into every token.
    public string Issuer { get; set; } = "SmartSolar.Api";

    // Audience claim written into every token.
    public string Audience { get; set; } = "SmartSolar.Clients";

    // Token lifetime in hours. REQ-AUTH §4 step 5 specifies 8 hours.
    public int ExpiryHours { get; set; } = 8;
}
