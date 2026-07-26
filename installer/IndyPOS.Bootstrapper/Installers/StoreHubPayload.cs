using System.IO.Compression;

namespace IndyPOS.Bootstrapper.Installers;

/// <summary>
/// Lays the StoreHub payload down over an install directory.
/// <para>USED BY BOTH INSTALL PATHS — fresh install and in-place upgrade.</para>
/// <para>Extraction is per-entry with <c>overwrite: true</c> and has no clean step, so the
/// result is old-union-new. Rollback must therefore delete before copying back
/// (see the upgrade design spec, section 5).</para>
/// </summary>
public static class StoreHubPayload
{
    public const string ResourceName = "IndyPOS.Bootstrapper.Resources.StoreHub.zip";

    /// <param name="resourceName">
    /// Overridable for tests only - production always uses the default. Lets tests point
    /// at a resource name guaranteed not to exist, so the "not found" branch is
    /// deterministic regardless of whether this assembly happens to have a
    /// StoreHub.zip embedded (e.g. after running build-installer.ps1).
    /// </param>
    /// <param name="probeDirectory">
    /// Overridable for tests only - null means the production default, <see cref="AppContext.BaseDirectory"/>.
    /// Lets tests point the external-zip/external-folder probes at an empty temp
    /// directory instead of the real test-bin output directory.
    /// </param>
    public static async Task<bool> ExtractAsync(
        string destinationPath,
        IProgress<string>? log = null,
        CancellationToken cancellationToken = default,
        string resourceName = ResourceName,
        string? probeDirectory = null)
    {
        var baseDirectory = probeDirectory ?? AppContext.BaseDirectory;
        var assembly = typeof(StoreHubPayload).Assembly;

        using var resourceStream = assembly.GetManifestResourceStream(resourceName);

        if (resourceStream != null)
        {
            log?.Report("Extracting from embedded resources...");

            using var archive = new ZipArchive(resourceStream, ZipArchiveMode.Read);
            foreach (var entry in archive.Entries)
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (string.IsNullOrEmpty(entry.Name))
                {
                    continue;
                }

                var destPath = Path.Combine(destinationPath, entry.FullName);
                var destDir = Path.GetDirectoryName(destPath);

                if (!string.IsNullOrEmpty(destDir) && !Directory.Exists(destDir))
                {
                    Directory.CreateDirectory(destDir);
                }

                entry.ExtractToFile(destPath, overwrite: true);
            }

            return true;
        }

        var externalZip = Path.Combine(baseDirectory, "StoreHub.zip");

        if (File.Exists(externalZip))
        {
            log?.Report("Extracting from external package...");
            ZipFile.ExtractToDirectory(externalZip, destinationPath, overwriteFiles: true);
            return true;
        }

        var externalFolder = Path.Combine(baseDirectory, "StoreHub");

        if (Directory.Exists(externalFolder))
        {
            log?.Report("Copying from external folder...");
            await CopyDirectoryAsync(externalFolder, destinationPath, cancellationToken);
            return true;
        }

        log?.Report("ERROR: StoreHub binaries not found!");
        log?.Report("Expected locations:");
        log?.Report($"  - Embedded resource: {resourceName}");
        log?.Report($"  - External zip: {externalZip}");
        log?.Report($"  - External folder: {externalFolder}");

        return false;
    }

    private static async Task CopyDirectoryAsync(
        string sourceDir,
        string destDir,
        CancellationToken cancellationToken)
    {
        if (!Directory.Exists(destDir))
        {
            Directory.CreateDirectory(destDir);
        }

        foreach (var file in Directory.GetFiles(sourceDir))
        {
            cancellationToken.ThrowIfCancellationRequested();

            var destFile = Path.Combine(destDir, Path.GetFileName(file));
            await using var sourceStream = File.OpenRead(file);
            await using var destStream = File.Create(destFile);
            await sourceStream.CopyToAsync(destStream, cancellationToken);
        }

        foreach (var dir in Directory.GetDirectories(sourceDir))
        {
            var destSubDir = Path.Combine(destDir, Path.GetFileName(dir));
            await CopyDirectoryAsync(dir, destSubDir, cancellationToken);
        }
    }
}
