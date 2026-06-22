using System.Runtime.Versioning;
using FluentAssertions;
using IndyPOS.Application.Common.Models;
using IndyPOS.StoreHub.Configuration;
using IndyPOS.Vault;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace IndyPOS.StoreHub.IntegrationTests.Configuration;

[SupportedOSPlatform("windows")]
public class SecretConfigurationExtensionsTests
{
    private const string ConnKey = "ConnectionStrings:storehub-db";
    private const string JwtKey = "LocalToken:SecretKey";

    private static ConfigurationManager BuildConfig(Dictionary<string, string?> values)
    {
        var config = new ConfigurationManager();
        config.AddInMemoryCollection(values);
        return config;
    }

    [Fact]
    public void UnprotectSecrets_WithProtectedValue_ShouldExposePlaintextViaGetConnectionString()
    {
        const string conn = "Host=127.0.0.1;Port=5432;Database=db;Username=u;Password=p";
        var config = BuildConfig(new() { [ConnKey] = SecretProtector.Protect(ConnKey, conn) });

        config.UnprotectSecrets(ConnKey, JwtKey);

        config.GetConnectionString("storehub-db").Should().Be(conn);
    }

    [Fact]
    public void UnprotectSecrets_WithProtectedValue_ShouldExposePlaintextViaLocalTokenOptionsBinding()
    {
        const string secret = "a-64-byte-base64-signing-key-value-for-testing-purposes==";
        var config = BuildConfig(new() { [JwtKey] = SecretProtector.Protect(JwtKey, secret) });

        config.UnprotectSecrets(ConnKey, JwtKey);

        var options = config.GetSection(LocalTokenOptions.SectionName).Get<LocalTokenOptions>();
        options!.SecretKey.Should().Be(secret);
    }

    [Fact]
    public void UnprotectSecrets_WithUnmarkedValue_ShouldLeaveConfigurationUnchanged()
    {
        const string conn = "Host=127.0.0.1;Password=plain";
        var config = BuildConfig(new() { [ConnKey] = conn });

        config.UnprotectSecrets(ConnKey, JwtKey);

        config[ConnKey].Should().Be(conn);
    }

    [Fact]
    public void UnprotectSecrets_WithMissingKey_ShouldNoOp()
    {
        var config = BuildConfig(new() { ["Unrelated"] = "x" });

        var act = () => config.UnprotectSecrets(ConnKey, JwtKey);

        act.Should().NotThrow();
    }

    [Fact]
    public void UnprotectSecrets_WithOneKeyProtectedAndOnePlaintext_ShouldDecryptOnlyProtected()
    {
        const string conn = "Host=127.0.0.1;Password=p";
        const string plainJwt = "plain-jwt";
        var config = BuildConfig(new()
        {
            [ConnKey] = SecretProtector.Protect(ConnKey, conn),
            [JwtKey] = plainJwt
        });

        config.UnprotectSecrets(ConnKey, JwtKey);

        config.GetConnectionString("storehub-db").Should().Be(conn);
        config[JwtKey].Should().Be(plainJwt);
    }

    [Fact]
    public void UnprotectSecrets_WhenDecryptFails_ShouldThrowNamingTheKey()
    {
        var corrupt = "DPAPI:" + Convert.ToBase64String(new byte[] { 1, 2, 3 });
        var config = BuildConfig(new() { [ConnKey] = corrupt });

        var act = () => config.UnprotectSecrets(ConnKey, JwtKey);

        act.Should().Throw<InvalidOperationException>()
           .WithMessage($"*{ConnKey}*");
    }

    [Fact]
    public void UnprotectSecrets_WhenDecryptFails_ShouldNotExposeRawCiphertext()
    {
        var corrupt = "DPAPI:" + Convert.ToBase64String(new byte[] { 1, 2, 3 });
        var config = BuildConfig(new() { [ConnKey] = corrupt });

        var act = () => config.UnprotectSecrets(ConnKey, JwtKey);

        act.Should().Throw<InvalidOperationException>()
           .Which.Message.Should().NotContain(corrupt);
    }
}
