using System.Runtime.Versioning;
using System.Security.Cryptography;
using FluentAssertions;
using IndyPOS.Vault;
using Xunit;

namespace IndyPOS.Vault.Tests;

[SupportedOSPlatform("windows")]
public class SecretProtectorTests
{
    private const string Key = "ConnectionStrings:storehub-db";

    [Theory]
    [InlineData("Host=127.0.0.1;Port=5432;Database=indypos_storehub;Username=indypos_app;Password=Ab12Cd34")]
    [InlineData("dGhpcyBpcyBhIDY0IGJ5dGUga2V5IGV4YW1wbGUgZm9yIHRlc3RpbmcgcHVycG9zZXM=")]
    [InlineData("ünîçödé-secret-Ω")]
    [InlineData("")]
    public void Protect_ThenUnprotect_ShouldRoundTripVariousPayloads(string plaintext)
    {
        var protectedValue = SecretProtector.Protect(Key, plaintext);

        SecretProtector.Unprotect(Key, protectedValue).Should().Be(plaintext);
    }

    [Fact]
    public void Protect_ShouldProduceMarkerPrefixedValue()
    {
        var result = SecretProtector.Protect(Key, "secret");

        result.Should().StartWith("DPAPI:");
    }

    [Fact]
    public void Unprotect_WithUnmarkedValue_ShouldReturnInputUnchanged()
    {
        const string plain = "Host=127.0.0.1;Password=plain";

        SecretProtector.Unprotect(Key, plain).Should().Be(plain);
    }

    [Theory]
    [InlineData("DPAPI:abc", true)]
    [InlineData("dpapi:abc", false)]
    [InlineData(" DPAPI:abc", false)]
    [InlineData("plain DPAPI: value", false)]
    [InlineData("", false)]
    public void IsProtected_WithVariousValues_ShouldReturnExpected(string value, bool expected)
    {
        SecretProtector.IsProtected(value).Should().Be(expected);
    }

    [Fact]
    public void Protect_OnAlreadyProtectedValue_ShouldThrow()
    {
        var once = SecretProtector.Protect(Key, "secret");

        var act = () => SecretProtector.Protect(Key, once);

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Protect_WithNullPlaintext_ShouldThrowArgumentNullException()
    {
        var act = () => SecretProtector.Protect(Key, null!);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Unprotect_WithMarkerAndNonBase64Payload_ShouldThrowFormatException()
    {
        var act = () => SecretProtector.Unprotect(Key, "DPAPI:!!!not-base64!!!");

        act.Should().Throw<FormatException>();
    }

    [Fact]
    public void Unprotect_WithValidBase64ButNonDpapiBytes_ShouldThrowCryptographicException()
    {
        var bogus = "DPAPI:" + Convert.ToBase64String(new byte[] { 1, 2, 3, 4, 5 });

        var act = () => SecretProtector.Unprotect(Key, bogus);

        act.Should().Throw<CryptographicException>();
    }

    [Fact]
    public void Unprotect_WithWrongEntropyKey_ShouldThrow()
    {
        var protectedValue = SecretProtector.Protect("LocalToken:SecretKey", "secret");

        var act = () => SecretProtector.Unprotect("ConnectionStrings:storehub-db", protectedValue);

        act.Should().Throw<CryptographicException>();
    }
}
