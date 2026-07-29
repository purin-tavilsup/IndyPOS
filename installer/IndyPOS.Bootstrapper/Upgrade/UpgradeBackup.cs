using IndyPOS.Bootstrapper.Installers;
using Npgsql;

namespace IndyPOS.Bootstrapper.Upgrade;

public sealed record BackupResult(bool Success, string? StampDirectory, bool Locked, string? ErrorMessage);

/// <summary>
/// Takes and restores the pre-upgrade recovery artifacts (see the upgrade design spec,
/// sections 4, 5 and 6). Everything here runs ABOVE the mutation line except
/// <see cref="RestoreStoreHubTree"/>, which runs on the rollback path.
/// </summary>
public sealed class UpgradeBackup(
    string backupsDirectory,
    string storeHubInstallPath,
    IProcessRunner processRunner,
    Func<string> stampFactory,
    Func<string, bool>? lockPath = null,
    Func<string, bool>? lockDirectory = null)
{
    public const string DumpFileName = "storehub.dump";

    /// <summary>A pg_dump that exits 0 having written less than this did not really succeed.</summary>
    public const int MinimumDumpBytes = 1024;

    /// <summary>Roughly 130 MB per stamp, forever, on a small unattended retail PC.</summary>
    public const int RetainedStamps = 2;

    private readonly Func<string, bool> _lockPath =
        lockPath ?? DatabaseSetup.TryRestrictFilePermissions;

    // Directories need their own helper: a non-inheritable ACE on a protected directory
    // leaves every child with an empty DACL. See TryRestrictDirectoryPermissions.
    private readonly Func<string, bool> _lockDirectory =
        lockDirectory ?? DatabaseSetup.TryRestrictDirectoryPermissions;

    public async Task<BackupResult> CreateAsync(
        string pgDumpPath,
        string connectionString,
        CancellationToken cancellationToken = default)
    {
        var stampDir = Path.Combine(backupsDirectory, stampFactory());

        try
        {
            // BackupsDirectory exists in InstallationConfig and the manifest, but only
            // DatabaseSetup.CreateDirectories guarantees it — and that never runs here.
            Directory.CreateDirectory(stampDir);

            var dumpPath = Path.Combine(stampDir, DumpFileName);
            var dumpFailure = await RunPgDumpAsync(pgDumpPath, connectionString, dumpPath, cancellationToken);
            if (dumpFailure is not null)
            {
                return new BackupResult(false, stampDir, false, dumpFailure);
            }

            var treeDestination = Path.Combine(stampDir, "StoreHub");
            CopyDirectory(storeHubInstallPath, treeDestination);

            var locked = LockTree(stampDir, dumpPath, treeDestination);

            Prune();

            return new BackupResult(true, stampDir, locked, null);
        }
        catch (Exception ex)
        {
            return new BackupResult(false, stampDir, false, ex.Message);
        }
    }

    /// <returns>An error message, or null on success.</returns>
    private async Task<string?> RunPgDumpAsync(
        string pgDumpPath, string connectionString, string dumpPath, CancellationToken cancellationToken)
    {
        // NpgsqlConnectionStringBuilder, not string splitting: the password may contain
        // anything, and a mis-split silently dumps the wrong database.
        var builder = new NpgsqlConnectionStringBuilder(connectionString);

        var arguments =
            $"--host={builder.Host} --port={builder.Port} --username={builder.Username} " +
            $"--dbname={builder.Database} --no-password -Fc -f \"{dumpPath}\"";

        // PGPASSWORD via the environment, never the command line: a command line is
        // visible to every local process.
        var environment = new Dictionary<string, string> { ["PGPASSWORD"] = builder.Password ?? "" };

        var run = await processRunner.RunAsync(pgDumpPath, arguments, environment, cancellationToken);

        if (run.ExitCode != 0)
        {
            return $"pg_dump failed (exit {run.ExitCode}): {run.StandardError.Trim()}";
        }

        if (!File.Exists(dumpPath))
        {
            return "pg_dump reported success but produced no file.";
        }

        var length = new FileInfo(dumpPath).Length;
        return length < MinimumDumpBytes
            ? $"pg_dump produced a file that is too small to be a valid dump ({length} bytes)."
            : null;
    }

    /// <summary>
    /// Locks the stamp directory with inheritable ACEs, so the dump and the whole tree are
    /// covered by inheritance rather than by walking hundreds of files.
    /// <para>The tree is NOT enumerated here. It used to be, and that is what failed on
    /// 2026-07-29: protecting the parent had already emptied the tree directory's DACL, so
    /// the walk hit UnauthorizedAccessException on a backup it had just written perfectly.
    /// Enumerating what you have deliberately made unreachable cannot work.</para>
    /// </summary>
    private bool LockTree(string stampDir, string dumpPath, string treeDestination)
    {
        // The dump is also locked explicitly: it is the whole sales history plus the BCrypt
        // admin hashes, so it should not depend on inheritance alone.
        return _lockDirectory(stampDir) & _lockPath(dumpPath) & _lockDirectory(treeDestination);
    }

    /// <summary>
    /// Puts the backed-up StoreHub tree back. DELETE-then-copy: extraction overwrites
    /// per entry with no clean step, so the failed tree is old-union-new and copying over
    /// it would strand new-version files (see the design spec, section 5).
    /// </summary>
    public void RestoreStoreHubTree(string stampDirectory)
    {
        var source = Path.Combine(stampDirectory, "StoreHub");

        if (!Directory.Exists(source))
        {
            throw new DirectoryNotFoundException(
                $"No StoreHub backup at {source}; cannot roll back. The database dump, if any, " +
                $"is still at {Path.Combine(stampDirectory, DumpFileName)}.");
        }

        if (Directory.Exists(storeHubInstallPath))
        {
            Directory.Delete(storeHubInstallPath, recursive: true);
        }

        CopyDirectory(source, storeHubInstallPath);
    }

    /// <summary>Keeps the newest <see cref="RetainedStamps"/> stamps. Returns how many it removed.</summary>
    public int Prune()
    {
        if (!Directory.Exists(backupsDirectory))
        {
            return 0;
        }

        // Stamp names are yyyyMMdd-HHmmss, so ordinal descending IS newest-first, and it
        // does not depend on directory timestamps a restore may have rewritten.
        var stale = Directory.GetDirectories(backupsDirectory)
            .OrderByDescending(Path.GetFileName, StringComparer.Ordinal)
            .Skip(RetainedStamps)
            .ToList();

        foreach (var dir in stale)
        {
            try
            {
                Directory.Delete(dir, recursive: true);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                // Retention is housekeeping; never fail an upgrade over it.
            }
        }

        return stale.Count;
    }

    private static void CopyDirectory(string sourceDir, string destDir)
    {
        Directory.CreateDirectory(destDir);

        foreach (var file in Directory.GetFiles(sourceDir))
        {
            File.Copy(file, Path.Combine(destDir, Path.GetFileName(file)), overwrite: true);
        }

        foreach (var dir in Directory.GetDirectories(sourceDir))
        {
            CopyDirectory(dir, Path.Combine(destDir, Path.GetFileName(dir)));
        }
    }
}
