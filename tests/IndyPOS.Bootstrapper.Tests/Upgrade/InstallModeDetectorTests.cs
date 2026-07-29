using FluentAssertions;
using IndyPOS.Bootstrapper.Upgrade;
using IndyPOS.Domain.Enums;

namespace IndyPOS.Bootstrapper.Tests.Upgrade;

public class InstallModeDetectorTests
{
    private const string InstallPath = @"C:\ProgramData\IndyPOS\v4\StoreHub";

    private sealed record FakeProbe : IInstallProbe
    {
        public bool ManifestExists { get; init; }
        public string? ManifestInstallVersion { get; init; }
        public bool PostgresStoreDatabaseExists { get; init; }
        public string? ServiceImagePath { get; init; }
        public StoreHubConfigFacts Config { get; init; } = new(false, false, null, null);
    }

    private static FakeProbe HealthyUpgrade() => new()
    {
        ManifestExists = true,
        ManifestInstallVersion = "4.0.0",
        PostgresStoreDatabaseExists = true,
        ServiceImagePath = InstallPath + @"\IndyPOS.StoreHub.exe",
        Config = new StoreHubConfigFacts(true, true, "Rungrat-001", StoreType.Minimart)
    };

    [Fact]
    public void Detect_OnAMachineRunningOnlyV3_ShouldReturnFresh()
    {
        // The real deployment case for all three live stores: they run v3.7.0, which predates
        // this layout entirely - no v4 manifest, no versioned StoreHub config, SQLite rather
        // than a Postgres store database. v4 installs alongside it, so this must be Fresh;
        // Unusable would block every store, and Upgrade would be worse.
        var v3Machine = new FakeProbe
        {
            ManifestExists = false,
            ManifestInstallVersion = null,
            PostgresStoreDatabaseExists = false,
            ServiceImagePath = null,
            Config = new StoreHubConfigFacts(Exists: false, false, null, null)
        };

        var result = InstallModeDetector.Detect(v3Machine, InstallPath);

        result.Mode.Should().Be(InstallMode.Fresh);
    }

    [Fact]
    public void Detect_OnABareMachine_ShouldReturnFresh()
    {
        var result = InstallModeDetector.Detect(new FakeProbe(), InstallPath);

        result.Mode.Should().Be(InstallMode.Fresh);
    }

    [Fact]
    public void Detect_OnAHealthyInstall_ShouldReturnUpgradeWithEveryDetectedValue()
    {
        var result = InstallModeDetector.Detect(HealthyUpgrade(), InstallPath);

        result.Mode.Should().Be(InstallMode.Upgrade);
        result.InstalledVersion.Should().Be("4.0.0");
        result.StoreId.Should().Be("Rungrat-001");
        result.StoreType.Should().Be(StoreType.Minimart);
    }

    [Fact]
    public void Detect_WithAStoreDatabaseButNoManifestForThisMajor_ShouldReturnUnusable()
    {
        // Rule 1, and it must beat rule 3. A v5 installer on a live v4 store finds no
        // manifest under v5\ and would otherwise read as Fresh -- but DatabaseName is
        // NOT version-scoped, so that "side-by-side" install collides with the v4 database.
        var probe = new FakeProbe { PostgresStoreDatabaseExists = true, ManifestExists = false };

        var result = InstallModeDetector.Detect(probe, InstallPath);

        result.Mode.Should().Be(InstallMode.Unusable);
        result.Reason.Should().Contain("another IndyPOS major version");
    }

    [Fact]
    public void Detect_WithAStoreDatabaseAndNoManifest_ShouldNotRecommendTheCleanupScript()
    {
        var probe = new FakeProbe { PostgresStoreDatabaseExists = true, ManifestExists = false };

        InstallModeDetector.Detect(probe, InstallPath).Reason.Should().NotContain("cleanup-v4");
    }

    [Fact]
    public void Detect_WithAnEmptyStoreId_ShouldReturnUnusable()
    {
        // StoreIdentityService falls back to STORE-{MachineName}, and payment_method is
        // keyed on (StoreId, Code) -- the seeder would insert a second full catalogue and
        // every later sale would be written against it.
        var probe = HealthyUpgrade() with
        {
            Config = new StoreHubConfigFacts(true, true, null, StoreType.Minimart)
        };

        var result = InstallModeDetector.Detect(probe, InstallPath);

        result.Mode.Should().Be(InstallMode.Unusable);
        result.Reason.Should().Contain("Store:Id");
    }

    [Fact]
    public void Detect_WithAnAbsentStoreType_ShouldReturnUnusable()
    {
        // Added 2026-07-18. Absent binds to GeneralHardware -- the MOST permissive value --
        // which would silently re-enable PayLater and Hardware products on a Minimart.
        var probe = HealthyUpgrade() with
        {
            Config = new StoreHubConfigFacts(true, true, "Rungrat-001", null)
        };

        var result = InstallModeDetector.Detect(probe, InstallPath);

        result.Mode.Should().Be(InstallMode.Unusable);
        result.Reason.Should().Contain("Store:Type");
    }

    [Fact]
    public void Detect_WithAnUndecryptableConnectionString_ShouldReturnUnusable()
    {
        var probe = HealthyUpgrade() with
        {
            Config = new StoreHubConfigFacts(true, false, "Rungrat-001", StoreType.Minimart)
        };

        var result = InstallModeDetector.Detect(probe, InstallPath);

        result.Mode.Should().Be(InstallMode.Unusable);
        result.Reason.Should().Contain("connection string");
    }

    [Fact]
    public void Detect_WhenTheServiceIsNotRegistered_ShouldReturnUnusable()
    {
        var probe = HealthyUpgrade() with { ServiceImagePath = null };

        InstallModeDetector.Detect(probe, InstallPath).Mode.Should().Be(InstallMode.Unusable);
    }

    [Fact]
    public void Detect_WhenTheServicePointsSomewhereElse_ShouldReturnUnusable()
    {
        var probe = HealthyUpgrade() with
        {
            ServiceImagePath = @"C:\Some\Other\Place\IndyPOS.StoreHub.exe"
        };

        InstallModeDetector.Detect(probe, InstallPath).Mode.Should().Be(InstallMode.Unusable);
    }

    [Fact]
    public void Detect_WithAQuotedServiceImagePath_ShouldStillMatch()
    {
        // sc.exe records binPath with quotes when the path contains spaces.
        var probe = HealthyUpgrade() with
        {
            ServiceImagePath = "\"" + InstallPath + "\\IndyPOS.StoreHub.exe\""
        };

        InstallModeDetector.Detect(probe, InstallPath).Mode.Should().Be(InstallMode.Upgrade);
    }

    [Fact]
    public void Detect_WhenTheServicePointsToASiblingDirectoryWithASimilarName_ShouldReturnUnusable()
    {
        // "StoreHub" is a string prefix of "StoreHubOLD" -- a bare StartsWith(installPath)
        // would wrongly treat this sibling folder as resolving under the install path.
        var probe = HealthyUpgrade() with
        {
            ServiceImagePath = InstallPath + @"OLD\IndyPOS.StoreHub.exe"
        };

        InstallModeDetector.Detect(probe, InstallPath).Mode.Should().Be(InstallMode.Unusable);
    }

    [Fact]
    public void Detect_WithAManifestButNoConfig_ShouldReturnUnusable()
    {
        // Rule 4. This is the exact state the second failed VM run left behind.
        var probe = new FakeProbe
        {
            ManifestExists = true,
            ManifestInstallVersion = "4.0.0",
            ServiceImagePath = InstallPath + @"\IndyPOS.StoreHub.exe",
            Config = new StoreHubConfigFacts(true, false, null, null)
        };

        InstallModeDetector.Detect(probe, InstallPath).Mode.Should().Be(InstallMode.Unusable);
    }

    [Fact]
    public void Detect_WithConfigButNoManifest_ShouldReturnUnusableNotFresh()
    {
        // Rule 3 requires BOTH absent. A leftover config means this is not a bare machine.
        var probe = new FakeProbe
        {
            Config = new StoreHubConfigFacts(true, true, "Rungrat-001", StoreType.Minimart)
        };

        InstallModeDetector.Detect(probe, InstallPath).Mode.Should().Be(InstallMode.Unusable);
    }
}
