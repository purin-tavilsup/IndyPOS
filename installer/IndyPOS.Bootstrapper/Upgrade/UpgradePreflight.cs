using System.Diagnostics;
using IndyPOS.Bootstrapper.Installers;

namespace IndyPOS.Bootstrapper.Upgrade;

public sealed record PreflightResult(
    bool Ok,
    string? PgDumpPath,
    string? ConnectionString,
    string? FailureReason,
    bool IsDowngrade);

/// <summary>
/// Step 1 of the upgrade sequence. Everything here runs ABOVE the mutation line: a
/// failure leaves the store untouched and still serving its current version.
/// </summary>
public static class UpgradePreflight
{
    /// <summary>Enough headroom for the dump plus a full copy of the StoreHub tree.</summary>
    public const long RequiredFreeBytes = 2L * 1024 * 1024 * 1024;

    private const string PosProcessName = "IndyPOS.Windows.Forms";

    /// <summary>
    /// Refuse a downgrade; allow a same-version repair. Rollback restores binaries and
    /// config but not schema, so running older binaries against a newer schema is exactly
    /// the state the forward-only migration rule cannot protect.
    /// </summary>
    public static (bool Ok, string? Reason, bool IsDowngrade) CheckVersions(
        string? installedVersion, string installerVersion)
    {
        if (!Version.TryParse(installedVersion, out var installed) ||
            !Version.TryParse(installerVersion, out var installing))
        {
            return (true, null, false);
        }

        return installing < installed
            ? (false,
               $"This installer is version {installerVersion} but the machine already runs " +
               $"{installedVersion}. Downgrading is refused: it would leave older binaries " +
               "against a newer database schema.",
               true)
            : (true, null, false);
    }

    /// <summary>
    /// Two Velopack processes on one install root can leave the POS unlaunchable, and the
    /// POS app self-updates independently, so it may be mid-update already.
    /// </summary>
    public static bool IsPosAppRunning() =>
        Process.GetProcessesByName(PosProcessName).Length > 0;

    /// <summary>
    /// The manifest's PostgresBinPath can be stale, so fall back to probing — but assert
    /// the major matches, because the fallback happily returns 17 or 16.
    /// </summary>
    /// <param name="findInstalledBinPath">
    /// Seam for the machine-wide probe, so a test can express "no other PostgreSQL here".
    /// Without it these tests pass or fail according to whether the box running them
    /// happens to have PostgreSQL 18 — both this dev box and the test VM do.
    /// Defaults to <see cref="PostgresInstaller.FindPostgresBinPath"/>.
    /// </param>
    public static string? LocatePgDump(
        string manifestBinPath,
        int expectedMajor,
        Func<string?>? findInstalledBinPath = null)
    {
        var candidate = Probe(manifestBinPath, expectedMajor);
        if (candidate is not null)
        {
            return candidate;
        }

        var discovered = (findInstalledBinPath ?? PostgresInstaller.FindPostgresBinPath)();
        return discovered is null ? null : Probe(discovered, expectedMajor);
    }

    private static string? Probe(string binPath, int expectedMajor)
    {
        if (string.IsNullOrWhiteSpace(binPath))
        {
            return null;
        }

        var exe = Path.Combine(binPath, "pg_dump.exe");
        if (!File.Exists(exe))
        {
            return null;
        }

        // Layout is ...\PostgreSQL\<major>\bin, so the grandparent names the major.
        var majorDir = Path.GetFileName(Path.GetDirectoryName(binPath.TrimEnd(Path.DirectorySeparatorChar)));

        return int.TryParse(majorDir, out var major) && major == expectedMajor ? exe : null;
    }

    public static bool HasFreeSpace(string path)
    {
        try
        {
            return new DriveInfo(Path.GetPathRoot(Path.GetFullPath(path))!).AvailableFreeSpace
                   >= RequiredFreeBytes;
        }
        catch (Exception ex) when (ex is ArgumentException or IOException or UnauthorizedAccessException)
        {
            // Unknowable is not the same as insufficient; let the dump's own integrity
            // check be the backstop rather than blocking a legitimate upgrade.
            return true;
        }
    }
}
