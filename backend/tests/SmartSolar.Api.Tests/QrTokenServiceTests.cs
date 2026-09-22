// ---------------------------------------------------------------------------
//  File        : QrTokenServiceTests.cs
//  Project     : Smart Solar Microgrid Trading System - Web API tests
//  Description : Unit tests for QrTokenService secret configuration enforcement
//                and token generation per ADR-003.
// ---------------------------------------------------------------------------

using Microsoft.Extensions.Options;
using SmartSolar.Api.Configuration;
using SmartSolar.Api.Services;
using Xunit;

namespace SmartSolar.Api.Tests;

public class QrTokenServiceTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_ThrowsInvalidOperationException_WhenSigningSecretIsMissingOrWhitespace(string? secret)
    {
        var options = Options.Create(new QrSettings { SigningSecret = secret! });

        var ex = Assert.Throws<InvalidOperationException>(() => new QrTokenService(options));
        Assert.Equal("QR signing secret must be configured.", ex.Message);
    }

    [Fact]
    public void Constructor_Succeeds_WhenSigningSecretIsConfigured()
    {
        var options = Options.Create(new QrSettings { SigningSecret = "super-secret-signing-key-value" });

        var service = new QrTokenService(options);
        var token = service.GenerateToken("RS-0001");

        Assert.NotNull(token);
        Assert.Contains('.', token);
        var parts = token.Split('.');
        Assert.Equal(2, parts.Length);
        Assert.NotEmpty(parts[0]);
        Assert.NotEmpty(parts[1]);
    }
}
