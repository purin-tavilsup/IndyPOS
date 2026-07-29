using FluentAssertions;
using IndyPOS.Bootstrapper.Upgrade;

namespace IndyPOS.Bootstrapper.Tests.Upgrade;

public class UpgradePreflightTests : IDisposable
{
    private readonly string _dir =
        Path.Combine(Path.GetTempPath(), "indypos-preflight-" + Guid.NewGuid().ToString("N"));

    public UpgradePreflightTests() => Directory.CreateDirectory(_dir);

    public void Dispose()
    {
        if (Directory.Exists(_dir))
        {
            Directory.Delete(_dir, recursive: true);
        }
    }

    [Fact]
    public void CheckVersions_WhenTheInstallerIsNewer_ShouldPass()
    {
        var (ok, _, isDowngrade) = UpgradePreflight.CheckVersions("4.0.0", "4.1.0");

        ok.Should().BeTrue();
        isDowngrade.Should().BeFalse();
    }

    [Fact]
    public void CheckVersions_WithTheSameVersion_ShouldPassAsARepair()
    {
        // Directory.Build.props pins the version and needs a manual bump, so a
        // same-version run is the common case during development. The 2026-07-26 spike
        // measured it as a safe 2.5s repair.
        var (ok, _, isDowngrade) = UpgradePreflight.CheckVersions("4.0.0", "4.0.0");

        ok.Should().BeTrue();
        isDowngrade.Should().BeFalse();
    }

    [Fact]
    public void CheckVersions_WhenTheInstallerIsOlder_ShouldRefuse()
    {
        var (ok, reason, isDowngrade) = UpgradePreflight.CheckVersions("4.1.0", "4.0.0");

        ok.Should().BeFalse();
        isDowngrade.Should().BeTrue();
        reason.Should().Contain("4.1.0");
    }

    [Fact]
    public void CheckVersions_WithAnUnreadableInstalledVersion_ShouldPass()
    {
        // A manifest without installVersion is decorative, not a blocker.
        UpgradePreflight.CheckVersions(null, "4.0.0").Ok.Should().BeTrue();
    }

    [Fact]
    public void LocatePgDump_WhenTheManifestPathIsGood_ShouldUseIt()
    {
        var bin = Path.Combine(_dir, "18", "bin");
        Directory.CreateDirectory(bin);
        File.WriteAllText(Path.Combine(bin, "pg_dump.exe"), "");

        UpgradePreflight.LocatePgDump(bin, expectedMajor: 18)
            .Should().Be(Path.Combine(bin, "pg_dump.exe"));
    }

    // "This machine has no other PostgreSQL." Without this seam the two tests below assert
    // nothing on a box that happens to have PostgreSQL 18 installed — which both this dev
    // box and the test VM do.
    private static string? NoInstalledPostgres() => null;

    [Fact]
    public void LocatePgDump_WhenTheManifestPathIsStale_ShouldReturnNullRatherThanAWrongMajor()
    {
        // FindPostgresInstallation probes 18 -> 17 -> 16; a pg_dump from a different
        // major can refuse the dump outright, so assert the major rather than guess.
        var bin = Path.Combine(_dir, "16", "bin");
        Directory.CreateDirectory(bin);
        File.WriteAllText(Path.Combine(bin, "pg_dump.exe"), "");

        UpgradePreflight.LocatePgDump(bin, expectedMajor: 18, NoInstalledPostgres).Should().BeNull();
    }

    [Fact]
    public void LocatePgDump_WhenTheManifestPathDoesNotExist_ShouldReturnNull()
    {
        UpgradePreflight.LocatePgDump(Path.Combine(_dir, "nope"), expectedMajor: 18, NoInstalledPostgres)
            .Should().BeNull();
    }

    [Fact]
    public void LocatePgDump_WhenTheManifestPathIsStaleButPostgresIsFound_ShouldUseTheDiscoveredPath()
    {
        // The reason the fallback exists at all: a manifest written before a PostgreSQL
        // move still names the old directory.
        var discovered = Path.Combine(_dir, "18", "bin");
        Directory.CreateDirectory(discovered);
        File.WriteAllText(Path.Combine(discovered, "pg_dump.exe"), "");

        UpgradePreflight.LocatePgDump(Path.Combine(_dir, "nope"), expectedMajor: 18, () => discovered)
            .Should().Be(Path.Combine(discovered, "pg_dump.exe"));
    }

    [Fact]
    public void LocatePgDump_WhenTheDiscoveredPathIsAWrongMajor_ShouldStillReturnNull()
    {
        // The fallback probes 18 -> 17 -> 16, so it can hand back a major we did not ask
        // for. Accepting it would dump with a pg_dump that may refuse the server outright.
        var discovered = Path.Combine(_dir, "17", "bin");
        Directory.CreateDirectory(discovered);
        File.WriteAllText(Path.Combine(discovered, "pg_dump.exe"), "");

        UpgradePreflight.LocatePgDump(Path.Combine(_dir, "nope"), expectedMajor: 18, () => discovered)
            .Should().BeNull();
    }
}
