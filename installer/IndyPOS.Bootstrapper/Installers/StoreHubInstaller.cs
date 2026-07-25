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

    public StoreHubInstaller() { }

    // Test seam: lets tests exercise post-configuration methods (e.g.
    // ProvisionDatabaseAsync) without the heavyweight InstallAsync.
    internal StoreHubInstaller(InstallationConfig config) => _config = config;

    private InstallationConfig Config =>
        _config ?? throw new InvalidOperationException(
            "StoreHubInstaller has not been configured. Call InstallAsync before StartServiceAsync.");

    /// <summary>
    /// Install StoreHub binaries and register as Windows Service.
    /// </summary>
    public async Task<StoreHubInstallerResult> InstallAsync(
        InstallationConfig config,
        IProgress<string>? log = null,
        CancellationToken cancellationToken = default)
    {
        _config = config;

        ConfigSnapshot? configSnapshot = null;

        try
        {
            log?.Report("Creating installation directory...");
            if (!Directory.Exists(Config.StoreHubInstallPath))
            {
                Directory.CreateDirectory(Config.StoreHubInstallPath);
            }

            // Must precede extraction: on an in-place upgrade the running service holds
            // its own DLLs open, so extracting over them throws "being used by another
            // process". A clean install has no service and this is a no-op.
            log?.Report("Checking for existing service...");
            await StopExistingServiceAsync(cancellationToken);

            // Extraction overwrites appsettings.json with the package template and
            // DatabaseSetup only rewrites the real values later, so a failure in between
            // would strand an upgraded store on a config it cannot start from.
            configSnapshot = ConfigSnapshot.Capture(
                Path.Combine(Config.StoreHubInstallPath, "appsettings.json"));

            log?.Report("Extracting StoreHub binaries...");
            var extractResult = await ExtractStoreHubBinariesAsync(log, cancellationToken);
            if (!extractResult)
            {
                configSnapshot.Restore();

                return new StoreHubInstallerResult
                {
                    Success = false,
                    ErrorMessage = "Failed to extract StoreHub binaries"
                };
            }

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
            // The locked-DLL failure arrives here as an IOException, so this is the path
            // that actually protects a live store's config.
            configSnapshot?.Restore();

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

    /// <summary>
    /// Provision the StoreHub database by running the app's one-shot "migrate"
    /// command (applies EF migrations + seeds the initial admin, then exits).
    /// Doing this as a console step BEFORE the service starts keeps the first
    /// service start instant, so a fresh-DB schema build can't overrun the 30s
    /// SCM start timeout (error 1053).
    /// </summary>
    public async Task<ProvisionDatabaseResult> ProvisionDatabaseAsync(
        IProgress<string>? log = null,
        CancellationToken cancellationToken = default)
    {
        var exePath = Path.Combine(Config.StoreHubInstallPath, "IndyPOS.StoreHub.exe");
        if (!File.Exists(exePath))
        {
            return new ProvisionDatabaseResult
            {
                Success = false,
                ErrorMessage = $"StoreHub executable not found at {exePath}"
            };
        }

        try
        {
            log?.Report("Applying database migrations and seeding admin...");

            var psi = new ProcessStartInfo
            {
                FileName = exePath,
                Arguments = "migrate",
                WorkingDirectory = Config.StoreHubInstallPath,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };
            // Production is the host default when unset, but be explicit so a stray
            // dev env var can't divert provisioning to the EnsureCreated path.
            psi.Environment["ASPNETCORE_ENVIRONMENT"] = "Production";

            using var process = new Process { StartInfo = psi };
            process.Start();

            var stdoutTask = process.StandardOutput.ReadToEndAsync(cancellationToken);
            var stderrTask = process.StandardError.ReadToEndAsync(cancellationToken);
            await Task.WhenAll(stdoutTask, stderrTask);
            var stdout = await stdoutTask;
            var stderr = await stderrTask;
            await process.WaitForExitAsync(cancellationToken);

            if (process.ExitCode != 0)
            {
                return new ProvisionDatabaseResult
                {
                    Success = false,
                    ErrorMessage = $"Database provisioning failed (exit {process.ExitCode}): {stderr.Trim()}"
                };
            }

            return new ProvisionDatabaseResult
            {
                Success = true,
                AdminSeeded = ParseAdminSeeded(stdout)
            };
        }
        catch (Exception ex)
        {
            return new ProvisionDatabaseResult
            {
                Success = false,
                ErrorMessage = $"Database provisioning failed: {ex.Message}"
            };
        }
    }

    internal static bool ParseAdminSeeded(string stdout) =>
        stdout.Contains("ADMIN_SEEDED=true", StringComparison.OrdinalIgnoreCase);

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

/// <summary>
/// Result of database provisioning (migrations + admin seeding).
/// </summary>
public class ProvisionDatabaseResult
{
    public bool Success { get; init; }
    public string? ErrorMessage { get; init; }
    public bool AdminSeeded { get; init; }
}
