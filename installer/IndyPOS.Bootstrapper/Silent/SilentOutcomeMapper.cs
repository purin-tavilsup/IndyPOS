using IndyPOS.Bootstrapper.Upgrade;

namespace IndyPOS.Bootstrapper.Silent;

public abstract record SilentOutcome;

public sealed record InstallSucceeded(
    bool AdminSeeded, string? CredFile, bool CredLocked, bool ServiceStarted, bool HealthOk) : SilentOutcome;

public sealed record InstallFailed(string Message) : SilentOutcome;

public sealed record InstallTimedOut : SilentOutcome;

public sealed record UsageErrorOutcome(string Message) : SilentOutcome;

public sealed record NotElevatedOutcome : SilentOutcome;

/// <summary>
/// An in-place upgrade completed. Sibling of <see cref="InstallSucceeded"/> rather than a
/// widening of it: that record is a five-field positional type belonging to the frozen
/// fresh path, and an upgrade reports a genuinely different set of facts.
/// </summary>
public sealed record UpgradeSucceeded(
    string? FromVersion, string ToVersion, bool ServiceStarted, bool HealthOk,
    string BackupDir, bool BackupLocked, bool PosUpdated) : SilentOutcome;

/// <param name="ServiceStarted">Post-rollback when <paramref name="RolledBack"/> is true.</param>
/// <param name="HealthOk">Post-rollback when <paramref name="RolledBack"/> is true.</param>
public sealed record UpgradeFailed(
    string Message, bool RolledBack, bool ServiceStarted, bool HealthOk,
    string? BackupDir) : SilentOutcome;

public sealed record UnusableInstall(string Reason) : SilentOutcome;

public sealed record DowngradeRefused(string InstalledVersion, string InstallerVersion) : SilentOutcome;

/// <summary>
/// Pure mapping from a run's outcome to an exit code + the non-secret marker
/// lines. No I/O — every permutation is unit-testable without running an install.
/// </summary>
public static class SilentOutcomeMapper
{
    private const string Prefix = "INDYPOS_MARKER ";
    private const string ResetHint =
        "run \"IndyPOS.StoreHub.exe reset-admin\" from the StoreHub install dir to reissue a bootstrap password";

    public static (int ExitCode, IReadOnlyList<string> Markers) Map(SilentOutcome outcome) => outcome switch
    {
        InstallSucceeded s => (0, SuccessMarkers(s)),
        InstallFailed => (2, [Prefix + "RESULT=failed"]),
        InstallTimedOut => (4, [Prefix + "RESULT=timeout"]),
        UsageErrorOutcome u => (1, [Prefix + "RESULT=failed", Prefix + $"REASON={u.Message}"]),
        NotElevatedOutcome => (3, [Prefix + "RESULT=failed"]),
        UpgradeSucceeded u => (0, UpgradeSuccessMarkers(u)),
        UpgradeFailed u => (2, UpgradeFailureMarkers(u)),
        UnusableInstall u => (5, [Prefix + "RESULT=failed", Prefix + $"REASON={u.Reason}"]),
        DowngradeRefused d => (6, [
            Prefix + "RESULT=failed",
            Prefix + $"REASON=Installed version {d.InstalledVersion} is newer than this " +
                     $"installer ({d.InstallerVersion}); downgrade refused."]),
        _ => throw new ArgumentOutOfRangeException(nameof(outcome))
    };

    /// <summary>
    /// Written to the log by the router before either orchestrator runs, so months later a
    /// store's own install-latest.log answers "which path ran".
    /// </summary>
    public static string ModeMarker(InstallMode mode) =>
        Prefix + "MODE=" + mode.ToString().ToLowerInvariant();

    // No ADMIN_SEEDED / RESET_HINT / CRED_FILE here: those belong to the fresh path, and
    // on an upgrade the reset hint actively misleads (spec section 7).
    private static IReadOnlyList<string> UpgradeSuccessMarkers(UpgradeSucceeded u) =>
    [
        Prefix + "RESULT=success",
        Prefix + $"FROM_VERSION={u.FromVersion ?? "unknown"}",
        Prefix + $"TO_VERSION={u.ToVersion}",
        Prefix + $"SERVICE_STARTED={Lower(u.ServiceStarted)}",
        Prefix + $"HEALTH={(u.HealthOk ? "ok" : "failed")}",
        Prefix + $"BACKUP_DIR={u.BackupDir}",
        Prefix + $"BACKUP_LOCKED={Lower(u.BackupLocked)}",
        Prefix + $"POS_UPDATED={Lower(u.PosUpdated)}"
    ];

    private static IReadOnlyList<string> UpgradeFailureMarkers(UpgradeFailed u)
    {
        var markers = new List<string>
        {
            Prefix + "RESULT=failed",
            // Scrubbed: a pg_dump or Npgsql error can quote the connection string, and this
            // line lands in a log file that is read by whoever is standing at the till.
            Prefix + $"REASON={SecretScrubber.Scrub(u.Message)}",
            Prefix + $"ROLLED_BACK={Lower(u.RolledBack)}",
            Prefix + $"SERVICE_STARTED={Lower(u.ServiceStarted)}",
            Prefix + $"HEALTH={(u.HealthOk ? "ok" : "failed")}",
            Prefix + "POS_UPDATED=false"
        };

        if (u.BackupDir is not null)
        {
            markers.Add(Prefix + $"BACKUP_DIR={u.BackupDir}");
        }

        return markers;
    }

    private static IReadOnlyList<string> SuccessMarkers(InstallSucceeded s)
    {
        var markers = new List<string>
        {
            Prefix + "RESULT=success",
            Prefix + $"ADMIN_SEEDED={Lower(s.AdminSeeded)}"
        };

        if (s.AdminSeeded)
        {
            markers.Add(Prefix + $"CRED_FILE={s.CredFile}");
            markers.Add(Prefix + $"CRED_LOCKED={Lower(s.CredLocked)}");
        }
        else
        {
            markers.Add(Prefix + $"RESET_HINT={ResetHint}");
        }

        markers.Add(Prefix + $"SERVICE_STARTED={Lower(s.ServiceStarted)}");
        markers.Add(Prefix + $"HEALTH={(s.HealthOk ? "ok" : "failed")}");
        return markers;
    }

    private static string Lower(bool value) => value ? "true" : "false";
}
