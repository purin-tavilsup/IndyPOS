using IndyPOS.Bootstrapper.Installers;
using IndyPOS.Bootstrapper.Silent;

namespace IndyPOS.Bootstrapper.Upgrade;

/// <summary>A captured config, restorable once. Wraps ConfigSnapshot for testability.</summary>
public sealed class ConfigSnapshotHandle(Action restore)
{
    public void Restore() => restore();
}

/// <summary>
/// The individual step units the upgrade sequence drives. An interface so the ORDER and
/// the rollback decisions — the parts that actually failed in production — are unit-testable
/// without a service, a database or a 190 MB installer.
/// <para>There is deliberately no DatabaseSetup member: it is fresh-install only.</para>
/// </summary>
public interface IUpgradeSteps
{
    Task<PreflightResult> PreflightAsync(DetectedInstall detected, InstallationConfig config, CancellationToken ct);
    Task<bool> StopServiceAsync(CancellationToken ct);
    Task<BackupResult> BackupAsync(string pgDumpPath, string connectionString, CancellationToken ct);
    ConfigSnapshotHandle CaptureConfig();
    Task<bool> DeployAsync(CancellationToken ct);
    Task<MigrationRunResult> MigrateAsync(CancellationToken ct);
    Task<bool> StartServiceAsync(CancellationToken ct);
    Task<bool> HealthAsync(CancellationToken ct);
    string? ReadPosVersion();
    Task<bool> InstallPosAppAsync(CancellationToken ct);
    void RestoreStoreHubTree(string stampDirectory);
    Task WriteManifestAsync(CancellationToken ct);
}

/// <summary>
/// The in-place upgrade sequence (upgrade design spec, sections 4 and 5).
/// <para>Steps 0-3 mutate nothing: a failure there is a non-event. Steps 4-7 roll back.
/// Step 8 does not — the server is upgraded and serving, and the 2026-07-26 spike confirmed
/// the POS step cannot disturb it.</para>
/// </summary>
public sealed class UpgradeOrchestrator(IUpgradeSteps steps, UpgradeStage? simulateFailure = null)
{
    public async Task<SilentOutcome> RunAsync(
        DetectedInstall detected,
        InstallationConfig config,
        IProgress<InstallationProgress> progress,
        CancellationToken cancellationToken = default)
    {
        // --- Step 1: preflight ------------------------------------------------
        progress.Report(InstallationProgress.Step("Upgrading", "Checking this machine...", 5));

        var preflight = await steps.PreflightAsync(detected, config, cancellationToken);
        Fault(UpgradeStage.Preflight);

        if (!preflight.Ok)
        {
            return preflight.IsDowngrade
                ? new DowngradeRefused(detected.InstalledVersion ?? "unknown", config.InstallVersion)
                // Nothing was touched, but "still serving" is a claim, so prove it rather
                // than assume it - the same reason the rollback probes.
                : new UpgradeFailed(preflight.FailureReason ?? "Preflight failed.",
                    RolledBack: false, ServiceStarted: true,
                    HealthOk: await steps.HealthAsync(cancellationToken), BackupDir: null);
        }

        var posVersionBefore = steps.ReadPosVersion();

        // --- Step 2: stop the service ----------------------------------------
        // Above the mutation line: stopping is reversible, and dumping a live database
        // would silently discard any sale completed before the restart.
        progress.Report(InstallationProgress.Step("Upgrading", "Stopping StoreHub...", 10));

        // Terminal, not advisory: deploying while StoreHub still holds its own DLLs is
        // exactly how the original upgrade attempt died. The store is untouched and still
        // serving here, so refusing costs nothing.
        if (!await steps.StopServiceAsync(cancellationToken))
        {
            return new UpgradeFailed(
                "StoreHub could not be stopped, so its binaries are still locked and cannot " +
                "be replaced. Stop the service manually and re-run.",
                RolledBack: false, ServiceStarted: true,
                HealthOk: await steps.HealthAsync(cancellationToken), BackupDir: null);
        }

        // --- Step 3: back up --------------------------------------------------
        progress.Report(InstallationProgress.Step("Upgrading", "Backing up database and binaries...", 20));

        BackupResult backup;
        try
        {
            // Inside the try so the test hook cannot leave a VM store down either.
            Fault(UpgradeStage.Stop);

            backup = await steps.BackupAsync(preflight.PgDumpPath!, preflight.ConnectionString!, cancellationToken);
            Fault(UpgradeStage.Backup);
        }
        catch (Exception ex)
        {
            // The service is already stopped, so an exception here - a fired watchdog on a
            // large database, say - would otherwise leave the till DOWN with nothing mutated.
            // "Above the mutation line" has to mean the store is still trading.
            var reason = ex is OperationCanceledException
                ? "The upgrade timed out while backing up."
                : $"Backing up failed: {ex.Message}";

            return await ResumeWithoutChangesAsync(reason);
        }

        if (!backup.Success)
        {
            return await ResumeWithoutChangesAsync(backup.ErrorMessage ?? "Backup failed.");
        }

        // ============ EVERYTHING BELOW THIS LINE MUTATES THE INSTALL ============

        var snapshot = steps.CaptureConfig();

        try
        {
            // --- Step 4: deploy -----------------------------------------------
            progress.Report(InstallationProgress.Step("Upgrading", "Deploying StoreHub...", 40));
            Fault(UpgradeStage.Deploy);

            if (!await steps.DeployAsync(cancellationToken))
            {
                return await RollBackAsync("Failed to deploy the StoreHub payload.",
                    backup.StampDirectory!, snapshot, cancellationToken);
            }

            // --- Step 5: restore the store's real config ----------------------
            // The heart of the design. Extraction just wrote the package template over
            // appsettings.json, and DatabaseSetup — which would have rewritten the real
            // values on a fresh install — never runs here.
            progress.Report(InstallationProgress.Step("Upgrading", "Restoring store configuration...", 55));
            snapshot.Restore();
            Fault(UpgradeStage.RestoreConfig);

            // --- Step 6: migrate ----------------------------------------------
            progress.Report(InstallationProgress.Step("Upgrading", "Applying database migrations...", 65));
            Fault(UpgradeStage.Migrate);

            var migration = await steps.MigrateAsync(cancellationToken);
            if (!migration.Success)
            {
                return await RollBackAsync(migration.ErrorMessage ?? "Migration failed.",
                    backup.StampDirectory!, snapshot, cancellationToken);
            }

            // --- Step 7: start + verify ---------------------------------------
            progress.Report(InstallationProgress.Step("Upgrading", "Starting StoreHub...", 80));
            Fault(UpgradeStage.Start);

            var started = await steps.StartServiceAsync(cancellationToken);
            var healthy = started && await steps.HealthAsync(cancellationToken);

            if (!healthy)
            {
                return await RollBackAsync("StoreHub did not become healthy after the upgrade.",
                    backup.StampDirectory!, snapshot, cancellationToken);
            }
        }
        catch (Exception ex)
        {
            // Cancellation is NOT excluded here. The watchdog can fire below the mutation
            // line on a slow store PC, and letting it escape leaves new binaries, a stopped
            // service and no rollback - the worst state this design exists to prevent.
            var reason = ex is OperationCanceledException
                ? "The upgrade timed out."
                : ex.Message;

            // CancellationToken.None deliberately: recovery must not be cancellable by the
            // token that just fired, or every await in the rollback aborts immediately.
            return await RollBackAsync(reason, backup.StampDirectory!, snapshot, CancellationToken.None);
        }

        // ==================== SERVER UPGRADE COMPLETE ====================

        // --- Step 8: the POS app. No rollback past this point. ----------------
        progress.Report(InstallationProgress.Step("Upgrading", "Updating the POS application...", 90));
        Fault(UpgradeStage.PosApp);

        var posOk = await steps.InstallPosAppAsync(cancellationToken);
        if (!posOk)
        {
            return new UpgradeFailed(
                "The StoreHub service upgraded and is serving, but the POS application " +
                "update failed. Re-run the installer to retry it; the store can keep trading.",
                RolledBack: false, ServiceStarted: true, HealthOk: true, backup.StampDirectory);
        }

        // POS_UPDATED cannot come from the exit code: the spike measured 0 for both a real
        // upgrade and a same-version repair. sq.version is the only truthful record.
        var posUpdated = steps.ReadPosVersion() is { } after &&
                         !string.Equals(after, posVersionBefore, StringComparison.Ordinal);

        // --- Steps 9-10: manifest + markers. Non-fatal; the next run repairs a stale one.
        try
        {
            await steps.WriteManifestAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            progress.Report(InstallationProgress.Error($"Warning: could not write install manifest: {ex.Message}"));
        }

        progress.Report(InstallationProgress.Step("Upgrade Complete", "IndyPOS has been upgraded.", 100));

        return new UpgradeSucceeded(
            detected.InstalledVersion, config.InstallVersion,
            ServiceStarted: true, HealthOk: true,
            backup.StampDirectory!, backup.Locked, posUpdated);
    }

    /// <summary>
    /// Nothing was mutated, but the service was stopped - restart it and report what actually
    /// happened rather than assuming it came up. Non-cancellable: this runs on paths a fired
    /// watchdog reaches, and the store must not be left down.
    /// </summary>
    private async Task<UpgradeFailed> ResumeWithoutChangesAsync(string reason)
    {
        var restarted = await steps.StartServiceAsync(CancellationToken.None);

        return new UpgradeFailed(reason, RolledBack: false, ServiceStarted: restarted,
            HealthOk: restarted && await steps.HealthAsync(CancellationToken.None), BackupDir: null);
    }

    /// <summary>
    /// Delete-then-copy the binaries back, put the config back, restart, and PROVE it came
    /// back. Without the probe a rollback can report "the store resumed on its previous
    /// version" while the store is dead.
    /// </summary>
    private async Task<UpgradeFailed> RollBackAsync(
        string reason, string stampDirectory, ConfigSnapshotHandle snapshot, CancellationToken cancellationToken)
    {
        try
        {
            steps.RestoreStoreHubTree(stampDirectory);
        }
        catch (Exception ex)
        {
            return new UpgradeFailed(
                $"{reason} The rollback then failed as well ({ex.Message}). This store needs " +
                $"manual recovery; the database dump is at {stampDirectory}.",
                RolledBack: false, ServiceStarted: false, HealthOk: false, stampDirectory);
        }

        snapshot.Restore();

        var started = await steps.StartServiceAsync(cancellationToken);
        var healthy = started && await steps.HealthAsync(cancellationToken);

        var message = healthy
            ? $"{reason} The upgrade was rolled back and the store is serving its previous version."
            : $"{reason} The upgrade was rolled back but StoreHub is not healthy. Restore the " +
              $"database dump at {stampDirectory} — see docs\\operations\\upgrade-procedure.md.";

        return new UpgradeFailed(message, RolledBack: true, started, healthy, stampDirectory);
    }

    private void Fault(UpgradeStage stage)
    {
        if (simulateFailure == stage)
        {
            throw new InvalidOperationException($"Simulated failure at stage '{stage}' (test hook).");
        }
    }
}
