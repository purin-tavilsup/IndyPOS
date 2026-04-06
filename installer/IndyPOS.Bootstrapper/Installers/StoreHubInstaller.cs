using System.Diagnostics;
using System.IO.Compression;
using System.ServiceProcess;

namespace IndyPOS.Bootstrapper.Installers;

/// <summary>
/// Handles StoreHub deployment and Windows Service installation.
/// </summary>
public class StoreHubInstaller
{
    private const string ServiceName = "IndyPOS.StoreHub";
    private const string DisplayName = "IndyPOS StoreHub";
    private const string Description = "IndyPOS local API service for point-of-sale operations";
    private const string InstallPath = @"C:\Program Files\IndyPOS\StoreHub";

    /// <summary>
    /// Install StoreHub binaries and register as Windows Service.
    /// </summary>
    public async Task<StoreHubInstallerResult> InstallAsync(
        InstallationConfig config,
        string jwtSecret,
        IProgress<string>? log = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Step 1: Ensure install directory exists
            log?.Report("Creating installation directory...");
            if (!Directory.Exists(InstallPath))
            {
                Directory.CreateDirectory(InstallPath);
            }

            // Step 2: Extract/copy StoreHub binaries
            log?.Report("Extracting StoreHub binaries...");
            var extractResult = await ExtractStoreHubBinariesAsync(log, cancellationToken);
            if (!extractResult)
            {
                return new StoreHubInstallerResult
                {
                    Success = false,
                    ErrorMessage = "Failed to extract StoreHub binaries"
                };
            }

            // Step 3: Stop existing service if running
            log?.Report("Checking for existing service...");
            await StopExistingServiceAsync(cancellationToken);

            // Step 4: Create/update Windows Service
            log?.Report("Registering Windows Service...");
            var serviceResult = await CreateWindowsServiceAsync(log, cancellationToken);
            if (!serviceResult)
            {
                return new StoreHubInstallerResult
                {
                    Success = false,
                    ErrorMessage = "Failed to create Windows Service"
                };
            }

            return new StoreHubInstallerResult { Success = true };
        }
        catch (Exception ex)
        {
            return new StoreHubInstallerResult
            {
                Success = false,
                ErrorMessage = ex.Message
            };
        }
    }

    /// <summary>
    /// Start the StoreHub Windows Service.
    /// </summary>
    public async Task<StoreHubInstallerResult> StartServiceAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            using var sc = new ServiceController(ServiceName);

            if (sc.Status == ServiceControllerStatus.Running)
            {
                return new StoreHubInstallerResult { Success = true };
            }

            sc.Start();

            // Wait for service to start (up to 60 seconds)
            var timeout = TimeSpan.FromSeconds(60);
            await Task.Run(() => sc.WaitForStatus(ServiceControllerStatus.Running, timeout), cancellationToken);

            return new StoreHubInstallerResult { Success = true };
        }
        catch (Exception ex)
        {
            return new StoreHubInstallerResult
            {
                Success = false,
                ErrorMessage = $"Failed to start service: {ex.Message}"
            };
        }
    }

    /// <summary>
    /// Extract StoreHub binaries from embedded resources or external source.
    /// </summary>
    private async Task<bool> ExtractStoreHubBinariesAsync(
        IProgress<string>? log,
        CancellationToken cancellationToken)
    {
        // Check if binaries are embedded as a resource
        var assembly = typeof(StoreHubInstaller).Assembly;
        var resourceName = "IndyPOS.Bootstrapper.Resources.StoreHub.zip";

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
                    continue; // Skip directories
                }

                var destPath = Path.Combine(InstallPath, entry.FullName);
                var destDir = Path.GetDirectoryName(destPath);

                if (!string.IsNullOrEmpty(destDir) && !Directory.Exists(destDir))
                {
                    Directory.CreateDirectory(destDir);
                }

                entry.ExtractToFile(destPath, overwrite: true);
            }

            return true;
        }

        // Check for external StoreHub.zip in same directory as bootstrapper
        var externalZip = Path.Combine(AppContext.BaseDirectory, "StoreHub.zip");

        if (File.Exists(externalZip))
        {
            log?.Report("Extracting from external package...");
            ZipFile.ExtractToDirectory(externalZip, InstallPath, overwriteFiles: true);
            return true;
        }

        // Check for StoreHub folder next to bootstrapper
        var externalFolder = Path.Combine(AppContext.BaseDirectory, "StoreHub");

        if (Directory.Exists(externalFolder))
        {
            log?.Report("Copying from external folder...");
            await CopyDirectoryAsync(externalFolder, InstallPath, cancellationToken);
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

    private static async Task StopExistingServiceAsync(CancellationToken cancellationToken)
    {
        try
        {
            using var sc = new ServiceController(ServiceName);

            if (sc.Status == ServiceControllerStatus.Running ||
                sc.Status == ServiceControllerStatus.StartPending)
            {
                sc.Stop();
                await Task.Run(() => sc.WaitForStatus(
                    ServiceControllerStatus.Stopped,
                    TimeSpan.FromSeconds(30)), cancellationToken);
            }
        }
        catch (InvalidOperationException)
        {
            // Service doesn't exist - that's fine
        }
    }

    private static async Task<bool> CreateWindowsServiceAsync(
        IProgress<string>? log,
        CancellationToken cancellationToken)
    {
        var exePath = Path.Combine(InstallPath, "IndyPOS.StoreHub.exe");

        if (!File.Exists(exePath))
        {
            log?.Report($"ERROR: Executable not found: {exePath}");
            return false;
        }

        // Check if service already exists
        var serviceExists = ServiceController.GetServices()
            .Any(s => s.ServiceName == ServiceName);

        if (serviceExists)
        {
            log?.Report("Service already exists, skipping creation");
            return true;
        }

        // Use sc.exe to create the service
        var psi = new ProcessStartInfo
        {
            FileName = "sc.exe",
            Arguments = $"create {ServiceName} " +
                        $"binPath= \"{exePath}\" " +
                        $"start= auto " +
                        $"DisplayName= \"{DisplayName}\"",
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };

        using var process = Process.Start(psi);
        if (process == null)
        {
            log?.Report("ERROR: Failed to start sc.exe");
            return false;
        }

        var output = await process.StandardOutput.ReadToEndAsync(cancellationToken);
        var error = await process.StandardError.ReadToEndAsync(cancellationToken);
        await process.WaitForExitAsync(cancellationToken);

        if (process.ExitCode != 0)
        {
            log?.Report($"ERROR: sc.exe failed: {error}");
            return false;
        }

        // Set service description
        var descPsi = new ProcessStartInfo
        {
            FileName = "sc.exe",
            Arguments = $"description {ServiceName} \"{Description}\"",
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var descProcess = Process.Start(descPsi);
        descProcess?.WaitForExit(5000);

        // Configure service recovery (restart on failure)
        var recoveryPsi = new ProcessStartInfo
        {
            FileName = "sc.exe",
            Arguments = $"failure {ServiceName} reset= 86400 actions= restart/5000/restart/10000/restart/30000",
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var recoveryProcess = Process.Start(recoveryPsi);
        recoveryProcess?.WaitForExit(5000);

        log?.Report("Windows Service created successfully");
        return true;
    }
}

/// <summary>
/// Result of StoreHub installation.
/// </summary>
public class StoreHubInstallerResult
{
    public bool Success { get; init; }
    public string? ErrorMessage { get; init; }
}
