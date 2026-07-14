using System.Text.Json;
using FluentAssertions;
using IndyPOS.Bootstrapper.Installers;

namespace IndyPOS.Bootstrapper.Tests.Installers;

public class InstallManifestTests
{
    [Fact]
    public void From_WithValidConfig_ShouldCopyAllFields()
    {
        var config = new InstallationConfig
        {
            StoreId = "STORE-001",
            AppPassword = "secret-not-in-manifest",
            AdminPassword = "admin-secret-not-in-manifest"
        };

        var manifest = InstallManifest.From(config);

        manifest.ManifestVersion.Should().Be(1);
        manifest.InstalledUtc.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        manifest.InstallVersion.Should().Be(config.InstallVersion);
        manifest.SystemRoot.Should().Be(config.SystemRoot);
        manifest.ConfigDirectory.Should().Be(config.ConfigDirectory);
        manifest.KeysDirectory.Should().Be(config.KeysDirectory);
        manifest.LogsDirectory.Should().Be(config.LogsDirectory);
        manifest.BackupsDirectory.Should().Be(config.BackupsDirectory);
        manifest.StoreHubInstallPath.Should().Be(config.StoreHubInstallPath);
        manifest.ServiceName.Should().Be(config.ServiceName);
        manifest.ServiceDisplayName.Should().Be(config.ServiceDisplayName);
        manifest.VelopackAppId.Should().Be(config.VelopackAppId);
        manifest.VelopackInstallPath.Should().Be(config.VelopackInstallPath);
        manifest.DatabaseName.Should().Be(config.DatabaseName);
        manifest.AppUser.Should().Be(config.AppUser);
        manifest.PostgresBinPath.Should().Be(config.PostgresBinPath);
        manifest.HealthCheckPort.Should().Be(config.HealthCheckPort);
    }

    [Fact]
    public void From_SerializedAsCamelCase_ShouldNotLeakAppPassword()
    {
        var config = new InstallationConfig
        {
            StoreId = "STORE-001",
            AppPassword = "ShouldNeverAppearInManifest!",
            AdminPassword = "AdminShouldNeverAppearInManifest!"
        };

        var manifest = InstallManifest.From(config);
        var options = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
        var json = JsonSerializer.Serialize(manifest, options);

        json.Should().NotContain("ShouldNeverAppearInManifest");
        json.Should().NotContain("appPassword", "the manifest schema must not carry credentials");
        json.Should().NotContain("adminPassword", "the manifest schema must not carry credentials");
        json.Should().Contain("\"serviceName\": \"IndyPOS.StoreHub.v4\"");
        json.Should().Contain("\"velopackAppId\": \"IndyPOS.POS.v4\"");
    }
}

public class InstallManifestWriterTests
{
    [Fact]
    public void ManifestFileName_ShouldBeStable()
    {
        // The cleanup-v4.ps1 script reads this exact filename.
        // If this constant changes, the PS script must change too.
        InstallManifestWriter.ManifestFileName.Should().Be("install-manifest.json");
    }
}
