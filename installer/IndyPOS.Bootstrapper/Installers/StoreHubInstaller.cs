using System.Diagnostics;
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
            var stopResult = await new ServiceControl(Config.ServiceName)
                .StopAsync(TimeSpan.FromSeconds(30), cancellationToken);
            if (!stopResult.Success)
            {
                // Not yet captured at this point in the sequence - nothing to restore -
                // but call it defensively so this stays correct if the ordering ever changes.
                configSnapshot?.Restore();

                return new StoreHubInstallerResult
                {
                    Success = false,
                    ErrorMessage = $"Failed to stop existing service '{Config.ServiceName}': {stopResult.ErrorMessage}"
                };
            }

            // Extraction overwrites appsettings.json with the package template and
            // DatabaseSetup only rewrites the real values later, so a failure in between
            // would strand an upgraded store on a config it cannot start from.
            configSnapshot = ConfigSnapshot.Capture(
                Path.Combine(Config.StoreHubInstallPath, "appsettings.json"));

            log?.Report("Extracting StoreHub binaries...");
            var extractResult = await StoreHubPayload.ExtractAsync(
                Config.StoreHubInstallPath, log, cancellationToken);
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
        var result = await new ServiceControl(Config.ServiceName)
            .StartAsync(TimeSpan.FromSeconds(60), cancellationToken);

        return result.Success
            ? new StoreHubInstallerResult { Success = true }
            : new StoreHubInstallerResult
            {
                Success = false,
                ErrorMessage = $"Failed to start service: {result.ErrorMessage}"
            };
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
