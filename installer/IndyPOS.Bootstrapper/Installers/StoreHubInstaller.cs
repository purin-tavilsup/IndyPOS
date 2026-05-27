using System.Diagnostics;
using System.IO.Compression;
using System.ServiceProcess;

namespace IndyPOS.Bootstrapper.Installers;

/// <summary>
/// Handles StoreHub deployment and Windows Service installation.
/// </summary>
public class StoreHubInstaller
{
    private InstallationConfig? _config;

    private InstallationConfig Config =>
        _config ?? throw new InvalidOperationException(
            "StoreHubInstaller has not been configured. Call InstallAsync before StartServiceAsync.");

    /// <summary>
    /// Install StoreHub binaries and register as Windows Service.
    /// </summary>
    public async Task<StoreHubInstallerResult> InstallAsync(
        InstallationConfig config,
        string jwtSecret,
        IProgress<string>? log = null,
        CancellationToken cancellationToken = default)
    {
        _config = config;

        try
        {
            log?.Report("Creating installation directory...");
            if (!Directory.Exists(Config.StoreHubInstallPath))
            {
                Directory.CreateDirectory(Config.StoreHubInstallPath);
            }

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

            log?.Report("Checking for existing service...");
            await StopExistingServiceAsync(cancellationToken);

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
            using var sc = new ServiceController(Config.ServiceName);

            if (sc.Status == ServiceControllerStatus.Running)
            {
                return new StoreHubInstallerResult { Success = true };
            }

            sc.Start();

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

    // Files that the bootstrapper generates at install time. The publish output
    // ships templates with placeholder values; we must NOT clobber the real
    // config written by DatabaseSetup.CreateStoreHubConfigAsync.
    private static readonly HashSet<string> RuntimeGeneratedFiles = new(StringComparer.OrdinalIgnoreCase)
    {
        "appsettings.Production.json"
    };

    private async Task<bool> ExtractStoreHubBinariesAsync(
        IProgress<string>? log,
        CancellationToken cancellationToken)
    {
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
                    continue;
                }

                if (RuntimeGeneratedFiles.Contains(entry.Name))
                {
                    log?.Report($"Skipping '{entry.Name}' (bootstrapper-generated, preserving real config)");
                    continue;
                }

                var destPath = Path.Combine(Config.StoreHubInstallPath, entry.FullName);
                var destDir = Path.GetDirectoryName(destPath);

                if (!string.IsNullOrEmpty(destDir) && !Directory.Exists(destDir))
                {
                    Directory.CreateDirectory(destDir);
                }

                entry.ExtractToFile(destPath, overwrite: true);
            }

            return true;
        }

        var externalZip = Path.Combine(AppContext.BaseDirectory, "StoreHub.zip");

        if (File.Exists(externalZip))
        {
            log?.Report("Extracting from external package...");
            ZipFile.ExtractToDirectory(externalZip, Config.StoreHubInstallPath, overwriteFiles: true);
            return true;
        }

        var externalFolder = Path.Combine(AppContext.BaseDirectory, "StoreHub");

        if (Directory.Exists(externalFolder))
        {
            log?.Report("Copying from external folder...");
            await CopyDirectoryAsync(externalFolder, Config.StoreHubInstallPath, cancellationToken);
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

    private async Task StopExistingServiceAsync(CancellationToken cancellationToken)
    {
        try
        {
            using var sc = new ServiceController(Config.ServiceName);

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

    private async Task<bool> CreateWindowsServiceAsync(
        IProgress<string>? log,
        CancellationToken cancellationToken)
    {
        var exePath = Path.Combine(Config.StoreHubInstallPath, "IndyPOS.StoreHub.exe");

        if (!File.Exists(exePath))
        {
            log?.Report($"ERROR: Executable not found: {exePath}");
            return false;
        }

        var serviceExists = ServiceController.GetServices()
            .Any(s => s.ServiceName == Config.ServiceName);

        if (serviceExists)
        {
            log?.Report("Service already exists, skipping creation");
            return true;
        }

        var psi = new ProcessStartInfo
        {
            FileName = "sc.exe",
            Arguments = $"create {Config.ServiceName} " +
                        $"binPath= \"{exePath}\" " +
                        $"start= auto " +
                        $"DisplayName= \"{Config.ServiceDisplayName}\"",
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

        var descPsi = new ProcessStartInfo
        {
            FileName = "sc.exe",
            Arguments = $"description {Config.ServiceName} \"{Config.ServiceDescription}\"",
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var descProcess = Process.Start(descPsi);
        descProcess?.WaitForExit(5000);

        var recoveryPsi = new ProcessStartInfo
        {
            FileName = "sc.exe",
            Arguments = $"failure {Config.ServiceName} reset= 86400 actions= restart/5000/restart/10000/restart/30000",
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
