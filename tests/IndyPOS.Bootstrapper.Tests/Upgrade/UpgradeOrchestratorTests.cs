using FluentAssertions;
using IndyPOS.Bootstrapper.Installers;
using IndyPOS.Bootstrapper.Silent;
using IndyPOS.Bootstrapper.Upgrade;
using IndyPOS.Domain.Enums;

namespace IndyPOS.Bootstrapper.Tests.Upgrade;

public class UpgradeOrchestratorTests
{
    private const string BackupDir = @"C:\ProgramData\IndyPOS\v4\backups\20260726-010203";

    private static readonly DetectedInstall Detected = new(
        InstallMode.Upgrade, "4.0.0", "Rungrat-001", StoreType.Minimart, "ok");

    private static InstallationConfig Config() => new() { StoreId = "Rungrat-001", Interactive = false };

    private sealed class FakeSteps : IUpgradeSteps
    {
        public List<string> Calls { get; } = [];

        public bool PreflightOk { get; init; } = true;
        public string PreflightReason { get; init; } = "preflight failed";
        public bool IsDowngrade { get; init; }
        public bool BackupOk { get; init; } = true;
        public bool DeployOk { get; init; } = true;
        public bool MigrateOk { get; init; } = true;
        public bool StartOk { get; init; } = true;
        public bool HealthOk { get; set; } = true;
        public bool PostRollbackHealthOk { get; init; } = true;
        public bool PosOk { get; init; } = true;
        public string? PosVersionAfter { get; init; } = "4.1.0";
        public bool RestoreThrows { get; init; }

        // Keyed off the rollback itself, not a call count: when deploy fails, step 7 never
        // probes, so the rollback's probe IS the first one.
        private bool _rolledBack;

        public Task<PreflightResult> PreflightAsync(DetectedInstall d, InstallationConfig c, CancellationToken ct)
        {
            Calls.Add("preflight");
            return Task.FromResult(new PreflightResult(
                PreflightOk, @"C:\pg\bin\pg_dump.exe", "Host=127.0.0.1",
                PreflightOk ? null : PreflightReason, IsDowngrade));
        }

        public Task<bool> StopServiceAsync(CancellationToken ct)
        {
            Calls.Add("stop");
            return Task.FromResult(true);
        }

        public Task<BackupResult> BackupAsync(string pgDump, string conn, CancellationToken ct)
        {
            Calls.Add("backup");
            return Task.FromResult(new BackupResult(BackupOk, BackupDir, true,
                BackupOk ? null : "pg_dump failed"));
        }

        public ConfigSnapshotHandle CaptureConfig()
        {
            Calls.Add("capture");
            return new ConfigSnapshotHandle(() => Calls.Add("restore-config"));
        }

        public Task<bool> DeployAsync(CancellationToken ct)
        {
            Calls.Add("deploy");
            return Task.FromResult(DeployOk);
        }

        public Task<MigrationRunResult> MigrateAsync(CancellationToken ct)
        {
            Calls.Add("migrate");
            return Task.FromResult(new MigrationRunResult(MigrateOk, false, MigrateOk ? null : "42703"));
        }

        public Task<bool> StartServiceAsync(CancellationToken ct)
        {
            Calls.Add("start");
            return Task.FromResult(StartOk);
        }

        public Task<bool> HealthAsync(CancellationToken ct)
        {
            Calls.Add("health");
            return Task.FromResult(_rolledBack ? PostRollbackHealthOk : HealthOk);
        }

        public string? ReadPosVersion()
        {
            Calls.Add("pos-version");
            return Calls.Count(c => c == "pos-version") == 1 ? "4.0.0" : PosVersionAfter;
        }

        public Task<bool> InstallPosAppAsync(CancellationToken ct)
        {
            Calls.Add("pos-install");
            return Task.FromResult(PosOk);
        }

        public void RestoreStoreHubTree(string stampDir)
        {
            Calls.Add("restore-tree");
            if (RestoreThrows) throw new DirectoryNotFoundException("no backup");
            _rolledBack = true;
        }

        public Task WriteManifestAsync(CancellationToken ct)
        {
            Calls.Add("manifest");
            return Task.CompletedTask;
        }
    }

    private static async Task<SilentOutcome> Run(FakeSteps steps, UpgradeStage? fault = null) =>
        await new UpgradeOrchestrator(steps, fault).RunAsync(
            Detected, Config(), new Progress<InstallationProgress>(_ => { }), CancellationToken.None);

    [Fact]
    public async Task RunAsync_OnTheHappyPath_ShouldSucceed()
    {
        var outcome = await Run(new FakeSteps());

        outcome.Should().BeOfType<UpgradeSucceeded>()
            .Which.BackupDir.Should().Be(BackupDir);
    }

    [Fact]
    public async Task RunAsync_ShouldStopTheServiceBeforeDumping()
    {
        // A sale completed between the dump and the restart would exist only in the live
        // database, and "restore the dump" is the documented recovery -- so that window
        // would be silently discarded.
        var steps = new FakeSteps();

        await Run(steps);

        steps.Calls.IndexOf("stop").Should().BeLessThan(steps.Calls.IndexOf("backup"));
    }

    [Fact]
    public async Task RunAsync_ShouldRestoreConfigAfterDeploy()
    {
        // Extraction overwrites appsettings.json with the package template; step 5 is what
        // puts the store's real config back, and it must land before migrate reads it.
        var steps = new FakeSteps();

        await Run(steps);

        steps.Calls.IndexOf("deploy").Should().BeLessThan(steps.Calls.IndexOf("restore-config"));
        steps.Calls.IndexOf("restore-config").Should().BeLessThan(steps.Calls.IndexOf("migrate"));
    }

    [Fact]
    public async Task RunAsync_ShouldNeverCallDatabaseSetup()
    {
        // Enforced structurally: IUpgradeSteps exposes no such member. This test documents
        // the invariant so a future widening of the seam is a deliberate act.
        typeof(IUpgradeSteps).GetMethods().Select(m => m.Name)
            .Should().NotContain(n => n.Contains("DatabaseSetup", StringComparison.OrdinalIgnoreCase));

        await Task.CompletedTask;
    }

    [Fact]
    public async Task RunAsync_WhenPreflightFails_ShouldNotTouchAnything()
    {
        var steps = new FakeSteps { PreflightOk = false };

        var outcome = await Run(steps);

        outcome.Should().BeOfType<UpgradeFailed>().Which.RolledBack.Should().BeFalse();
        steps.Calls.Should().NotContain("stop");
        steps.Calls.Should().NotContain("deploy");
    }

    [Fact]
    public async Task RunAsync_OnADowngrade_ShouldReturnDowngradeRefused()
    {
        var outcome = await Run(new FakeSteps { PreflightOk = false, IsDowngrade = true });

        outcome.Should().BeOfType<DowngradeRefused>();
    }

    [Fact]
    public async Task RunAsync_WhenTheBackupFails_ShouldStopBeforeTheMutationLine()
    {
        var steps = new FakeSteps { BackupOk = false };

        var outcome = await Run(steps);

        outcome.Should().BeOfType<UpgradeFailed>().Which.RolledBack.Should().BeFalse();
        steps.Calls.Should().NotContain("deploy");
    }

    [Fact]
    public async Task RunAsync_WhenTheBackupFails_ShouldPutTheServiceBack()
    {
        // The service was stopped above the mutation line. Leaving it down turns a
        // harmless preflight-class failure into an outage.
        var steps = new FakeSteps { BackupOk = false };

        var failure = (UpgradeFailed)await Run(steps);

        steps.Calls.Should().Contain("start");
        failure.ServiceStarted.Should().BeTrue();
        failure.BackupDir.Should().BeNull();
    }

    [Fact]
    public async Task RunAsync_WhenDeployFails_ShouldRollBackAndProbeHealth()
    {
        var steps = new FakeSteps { DeployOk = false };

        var outcome = await Run(steps);

        var failure = outcome.Should().BeOfType<UpgradeFailed>().Subject;
        failure.RolledBack.Should().BeTrue();
        steps.Calls.Should().ContainInOrder("restore-tree", "restore-config", "start", "health");
    }

    [Fact]
    public async Task RunAsync_WhenMigrateFails_ShouldRollBack()
    {
        var steps = new FakeSteps { MigrateOk = false };

        var outcome = await Run(steps);

        outcome.Should().BeOfType<UpgradeFailed>().Which.RolledBack.Should().BeTrue();
        steps.Calls.Should().Contain("restore-tree");
    }

    [Fact]
    public async Task RunAsync_WhenHealthFailsAfterStart_ShouldRollBack()
    {
        var steps = new FakeSteps { HealthOk = false };

        var outcome = await Run(steps);

        outcome.Should().BeOfType<UpgradeFailed>().Which.RolledBack.Should().BeTrue();
    }

    [Fact]
    public async Task RunAsync_WhenTheServiceDoesNotStart_ShouldNotProbeHealthBeforeRollingBack()
    {
        // A dead service answers nothing; probing it just burns the retry budget.
        var steps = new FakeSteps { StartOk = false };

        var outcome = await Run(steps);

        outcome.Should().BeOfType<UpgradeFailed>().Which.RolledBack.Should().BeTrue();
        steps.Calls.Take(steps.Calls.IndexOf("restore-tree")).Should().NotContain("health");
    }

    [Fact]
    public async Task RunAsync_WhenTheRollbackItselfIsUnhealthy_ShouldReportItRatherThanClaimRecovery()
    {
        var steps = new FakeSteps { DeployOk = false, PostRollbackHealthOk = false };

        var failure = (UpgradeFailed)await Run(steps);

        failure.RolledBack.Should().BeTrue();
        failure.HealthOk.Should().BeFalse();
        failure.Message.Should().Contain(BackupDir);
    }

    [Fact]
    public async Task RunAsync_WhenTheBackupTreeIsGone_ShouldReportTheRollbackDidNotHappen()
    {
        var steps = new FakeSteps { DeployOk = false, RestoreThrows = true };

        var failure = (UpgradeFailed)await Run(steps);

        failure.RolledBack.Should().BeFalse();
    }

    [Fact]
    public async Task RunAsync_WhenThePosStepFails_ShouldNotRollBackTheServer()
    {
        // Spec section 5 row 8, confirmed by the 2026-07-26 spike: the service survives a
        // full POS package swap, so the two are genuinely independent.
        var steps = new FakeSteps { PosOk = false };

        var failure = (UpgradeFailed)await Run(steps);

        failure.RolledBack.Should().BeFalse();
        failure.ServiceStarted.Should().BeTrue();
        steps.Calls.Should().NotContain("restore-tree");
    }

    [Fact]
    public async Task RunAsync_WhenThePosVersionDidNotChange_ShouldReportPosUpdatedFalse()
    {
        // A same-version repair exits 0 too (spike, section 12), so the exit code cannot
        // be trusted -- only sq.version before vs after can.
        var steps = new FakeSteps { PosVersionAfter = "4.0.0" };

        var success = (UpgradeSucceeded)await Run(steps);

        success.PosUpdated.Should().BeFalse();
    }

    [Fact]
    public async Task RunAsync_WhenThePosVersionChanged_ShouldReportPosUpdatedTrue()
    {
        var success = (UpgradeSucceeded)await Run(new FakeSteps { PosVersionAfter = "4.1.0" });

        success.PosUpdated.Should().BeTrue();
    }

    [Fact]
    public async Task RunAsync_WithASimulatedDeployFailure_ShouldRollBack()
    {
        // The fault hook is runtime-selected so ONE 190 MB installer build serves VM
        // cases 1 and 2 in a single session.
        var steps = new FakeSteps();

        var outcome = await Run(steps, UpgradeStage.Deploy);

        outcome.Should().BeOfType<UpgradeFailed>().Which.RolledBack.Should().BeTrue();
    }

    [Theory]
    [InlineData("deploy", UpgradeStage.Deploy)]
    [InlineData("MIGRATE", UpgradeStage.Migrate)]
    [InlineData("start", UpgradeStage.Start)]
    [InlineData("nonsense", null)]
    [InlineData(null, null)]
    public void Parse_WithVariousValues_ShouldReturnExpected(string? value, UpgradeStage? expected)
    {
        SimulatedFailure.Parse(value).Should().Be(expected);
    }
}
