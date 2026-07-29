using FluentAssertions;
using IndyPOS.Bootstrapper.Silent;
using IndyPOS.Bootstrapper.Upgrade;

namespace IndyPOS.Bootstrapper.Tests.Silent;

public class SilentOutcomeMapperTests
{
    [Fact]
    public void Map_WithSeededLockedSuccess_ShouldReturnZeroAndAllMarkers()
    {
        var outcome = new InstallSucceeded(AdminSeeded: true, CredFile: @"C:\x\admin-credentials.txt",
            CredLocked: true, ServiceStarted: true, HealthOk: true);

        var (exit, markers) = SilentOutcomeMapper.Map(outcome);

        exit.Should().Be(0);
        markers.Should().Contain("INDYPOS_MARKER RESULT=success");
        markers.Should().Contain("INDYPOS_MARKER ADMIN_SEEDED=true");
        markers.Should().Contain(@"INDYPOS_MARKER CRED_FILE=C:\x\admin-credentials.txt");
        markers.Should().Contain("INDYPOS_MARKER CRED_LOCKED=true");
        markers.Should().Contain("INDYPOS_MARKER SERVICE_STARTED=true");
        markers.Should().Contain("INDYPOS_MARKER HEALTH=ok");
    }

    [Fact]
    public void Map_WithUnlockedCredFile_ShouldStayZeroButFlagUnlocked()
    {
        var outcome = new InstallSucceeded(true, @"C:\x\admin-credentials.txt", CredLocked: false, true, true);

        var (exit, markers) = SilentOutcomeMapper.Map(outcome);

        exit.Should().Be(0);
        markers.Should().Contain("INDYPOS_MARKER CRED_LOCKED=false");
    }

    [Fact]
    public void Map_WithAdminAlreadyExisting_ShouldOmitCredFileAndEmitResetHint()
    {
        var outcome = new InstallSucceeded(AdminSeeded: false, CredFile: null, CredLocked: false,
            ServiceStarted: true, HealthOk: false);

        var (exit, markers) = SilentOutcomeMapper.Map(outcome);

        exit.Should().Be(0);
        markers.Should().Contain("INDYPOS_MARKER ADMIN_SEEDED=false");
        markers.Should().NotContain(m => m.StartsWith("INDYPOS_MARKER CRED_FILE="));
        markers.Should().Contain(m => m.StartsWith("INDYPOS_MARKER RESET_HINT="));
        markers.Should().Contain("INDYPOS_MARKER HEALTH=failed");
    }

    [Fact]
    public void Map_WithInstallFailed_ShouldReturnTwoAndOnlyResultMarker()
    {
        var (exit, markers) = SilentOutcomeMapper.Map(new InstallFailed("boom"));

        exit.Should().Be(2);
        markers.Should().ContainSingle().Which.Should().Be("INDYPOS_MARKER RESULT=failed");
    }

    [Fact]
    public void Map_WithTimeout_ShouldReturnFour()
    {
        var (exit, markers) = SilentOutcomeMapper.Map(new InstallTimedOut());

        exit.Should().Be(4);
        markers.Should().ContainSingle().Which.Should().Be("INDYPOS_MARKER RESULT=timeout");
    }

    [Fact]
    public void Map_WithUsageError_ShouldReturnOneAndReasonMarker()
    {
        var (exit, markers) = SilentOutcomeMapper.Map(new UsageErrorOutcome("bad"));

        exit.Should().Be(1);
        markers.Should().Contain("INDYPOS_MARKER RESULT=failed");
        markers.Should().Contain("INDYPOS_MARKER REASON=bad");
    }

    [Fact]
    public void Map_WithNotElevated_ShouldReturnThree()
    {
        SilentOutcomeMapper.Map(new NotElevatedOutcome()).ExitCode.Should().Be(3);
    }

    [Fact]
    public void Map_WithUpgradeSucceeded_ShouldReturnZeroAndTheUpgradeMarkers()
    {
        var outcome = new UpgradeSucceeded("4.0.0", "4.1.0", ServiceStarted: true, HealthOk: true,
            BackupDir: @"C:\ProgramData\IndyPOS\v4\backups\20260726-010203", BackupLocked: true,
            PosUpdated: true);

        var (exit, markers) = SilentOutcomeMapper.Map(outcome);

        exit.Should().Be(0);
        markers.Should().Contain("INDYPOS_MARKER RESULT=success");
        markers.Should().Contain("INDYPOS_MARKER SERVICE_STARTED=true");
        markers.Should().Contain("INDYPOS_MARKER HEALTH=ok");
        markers.Should().Contain(@"INDYPOS_MARKER BACKUP_DIR=C:\ProgramData\IndyPOS\v4\backups\20260726-010203");
        markers.Should().Contain("INDYPOS_MARKER BACKUP_LOCKED=true");
        markers.Should().Contain("INDYPOS_MARKER POS_UPDATED=true");
        markers.Should().Contain("INDYPOS_MARKER FROM_VERSION=4.0.0");
        markers.Should().Contain("INDYPOS_MARKER TO_VERSION=4.1.0");
    }

    [Fact]
    public void Map_WithUpgradeSucceeded_ShouldSuppressTheFreshPathAdminMarkers()
    {
        // ADMIN_SEEDED is necessarily false on an upgrade, and its else-branch would
        // advise reissuing a bootstrap password for a store whose admin is fine.
        var outcome = new UpgradeSucceeded("4.0.0", "4.1.0", true, true, @"C:\b", true, true);

        var (_, markers) = SilentOutcomeMapper.Map(outcome);

        markers.Should().NotContain(m => m.StartsWith("INDYPOS_MARKER ADMIN_SEEDED="));
        markers.Should().NotContain(m => m.StartsWith("INDYPOS_MARKER RESET_HINT="));
        markers.Should().NotContain(m => m.StartsWith("INDYPOS_MARKER CRED_FILE="));
    }

    [Fact]
    public void Map_WithAnUnknownFromVersion_ShouldSayUnknownRatherThanEmpty()
    {
        // A manifest without installVersion still upgrades; an empty marker value reads
        // as a broken installer to whoever greps the log.
        var outcome = new UpgradeSucceeded(null, "4.1.0", true, true, @"C:\b", true, true);

        var (_, markers) = SilentOutcomeMapper.Map(outcome);

        markers.Should().Contain("INDYPOS_MARKER FROM_VERSION=unknown");
    }

    [Fact]
    public void Map_WithAPosOnlyFailure_ShouldStillReportTheServerAsServing()
    {
        // Spec section 5 row 8: no rollback -- the server is upgraded and serving. The 2026-07-26
        // spike confirmed the POS step cannot disturb the service.
        var outcome = new UpgradeFailed("Velopack exited 1", RolledBack: false,
            ServiceStarted: true, HealthOk: true, BackupDir: @"C:\b");

        var (exit, markers) = SilentOutcomeMapper.Map(outcome);

        exit.Should().Be(2);
        markers.Should().Contain("INDYPOS_MARKER RESULT=failed");
        markers.Should().Contain("INDYPOS_MARKER SERVICE_STARTED=true");
        markers.Should().Contain("INDYPOS_MARKER POS_UPDATED=false");
        markers.Should().Contain("INDYPOS_MARKER ROLLED_BACK=false");
    }

    [Fact]
    public void Map_WithARolledBackFailure_ShouldReportThePostRollbackHealth()
    {
        // Starting the service is not evidence it came back; without the probe a rollback
        // can claim the store resumed while the store is dead.
        var outcome = new UpgradeFailed("migrate failed", RolledBack: true,
            ServiceStarted: true, HealthOk: false, BackupDir: @"C:\b\20260726-010203");

        var (exit, markers) = SilentOutcomeMapper.Map(outcome);

        exit.Should().Be(2);
        markers.Should().Contain("INDYPOS_MARKER ROLLED_BACK=true");
        markers.Should().Contain("INDYPOS_MARKER HEALTH=failed");
        markers.Should().Contain(@"INDYPOS_MARKER BACKUP_DIR=C:\b\20260726-010203");
    }

    [Fact]
    public void Map_WithAFailureBeforeAnyBackup_ShouldOmitTheBackupDirMarker()
    {
        // Preflight failures happen above the mutation line, so there is no backup to
        // point an operator at.
        var outcome = new UpgradeFailed("pg_dump not found", RolledBack: false,
            ServiceStarted: true, HealthOk: true, BackupDir: null);

        var (_, markers) = SilentOutcomeMapper.Map(outcome);

        markers.Should().NotContain(m => m.StartsWith("INDYPOS_MARKER BACKUP_DIR="));
    }

    [Fact]
    public void Map_WithAnUnusableInstall_ShouldReturnFive()
    {
        var (exit, markers) = SilentOutcomeMapper.Map(new UnusableInstall("service missing"));

        exit.Should().Be(5);
        markers.Should().Contain("INDYPOS_MARKER RESULT=failed");
        markers.Should().Contain("INDYPOS_MARKER REASON=service missing");
    }

    [Fact]
    public void Map_WithADowngrade_ShouldReturnSix()
    {
        var (exit, markers) = SilentOutcomeMapper.Map(new DowngradeRefused("4.1.0", "4.0.0"));

        exit.Should().Be(6);
        markers.Should().Contain("INDYPOS_MARKER RESULT=failed");
        markers.Should().Contain(m => m.Contains("4.1.0") && m.Contains("4.0.0"));
    }

    [Theory]
    [InlineData(InstallMode.Fresh, "INDYPOS_MARKER MODE=fresh")]
    [InlineData(InstallMode.Upgrade, "INDYPOS_MARKER MODE=upgrade")]
    [InlineData(InstallMode.Unusable, "INDYPOS_MARKER MODE=unusable")]
    public void ModeMarker_ForEachMode_ShouldEmitTheLowercaseName(InstallMode mode, string expected)
    {
        // Months later, a store's own install-latest.log must answer "which path ran"
        // without anyone reading C#.
        SilentOutcomeMapper.ModeMarker(mode).Should().Be(expected);
    }

    [Fact]
    public void Map_ForEveryOutcomeType_ShouldHaveAnExplicitArm()
    {
        // Map ends in a throwing discard, so a new outcome silently crashes the run.
        var outcomes = new SilentOutcome[]
        {
            new InstallSucceeded(true, @"C:\x", true, true, true),
            new InstallFailed("x"),
            new InstallTimedOut(),
            new UsageErrorOutcome("x"),
            new NotElevatedOutcome(),
            new UpgradeSucceeded("4.0.0", "4.1.0", true, true, @"C:\b", true, true),
            new UpgradeFailed("x", false, true, true, @"C:\b"),
            new UnusableInstall("x"),
            new DowngradeRefused("4.1.0", "4.0.0")
        };

        foreach (var outcome in outcomes)
        {
            var act = () => SilentOutcomeMapper.Map(outcome);
            act.Should().NotThrow($"{outcome.GetType().Name} needs an explicit arm");
        }
    }
}
