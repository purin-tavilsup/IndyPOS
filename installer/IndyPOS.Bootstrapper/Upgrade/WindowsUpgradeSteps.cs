using IndyPOS.Bootstrapper.Installers;

namespace IndyPOS.Bootstrapper.Upgrade;

/// <summary>Binds <see cref="IUpgradeSteps"/> to the real machine.</summary>
public sealed class WindowsUpgradeSteps(InstallationConfig config, IProgress<string> log) : IUpgradeSteps
{
    private const int PostgresMajor = 18;

    private static readonly TimeSpan ServiceTimeout = TimeSpan.FromSeconds(60);

    private readonly ServiceControl _service = new(config.ServiceName);

    private readonly UpgradeBackup _backup = new(
        config.BackupsDirectory,
        config.StoreHubInstallPath,
        new ExternalProcessRunner(),
        () => DateTime.Now.ToString("yyyyMMdd-HHmmss"));

    private string ConfigPath => Path.Combine(config.StoreHubInstallPath, "appsettings.json");

    public async Task<PreflightResult> PreflightAsync(
        DetectedInstall detected, InstallationConfig cfg, CancellationToken ct)
    {
        var (ok, reason, isDowngrade) = UpgradePreflight.CheckVersions(
            detected.InstalledVersion, cfg.InstallVersion);
        if (!ok)
        {
            return new PreflightResult(false, null, null, reason, isDowngrade);
        }

        if (UpgradePreflight.IsPosAppRunning())
        {
            return new PreflightResult(false, null, null,
                "The IndyPOS POS application is running. Close it on this machine and re-run.", false);
        }

        if (!UpgradePreflight.HasFreeSpace(cfg.BackupsDirectory))
        {
            return new PreflightResult(false, null, null,
                "Not enough free disk space to back up the database and binaries.", false);
        }

        var pgDump = UpgradePreflight.LocatePgDump(cfg.PostgresBinPath, PostgresMajor);
        if (pgDump is null)
        {
            return new PreflightResult(false, null, null,
                $"pg_dump.exe for PostgreSQL {PostgresMajor} could not be located.", false);
        }

        var connectionString = StoreHubConfigReader.ReadConnectionString(ConfigPath);
        if (connectionString is null)
        {
            return new PreflightResult(false, null, null,
                "The StoreHub connection string could not be decrypted on this machine.", false);
        }

        // Idempotent check-then-install, and the results are checked: the whole point of
        // running these above the mutation line is that a missing runtime fails here, for
        // free, instead of surfacing as a service that will not start after the swap.
        log.Report("Ensuring prerequisites (fonts, .NET 10, VC++ redistributable)...");

        // The font is the one genuinely optional prerequisite - a missing FC Subject renders
        // in a fallback face, which is a cosmetic regression, not a dead store.
        new FontInstaller().Install(log);

        var dotNet = await new DotNetInstaller().EnsureInstalledAsync(log, ct, cfg.Interactive);
        if (!dotNet.Success)
        {
            return new PreflightResult(false, null, null,
                $"The .NET 10 Desktop Runtime is required but could not be installed: " +
                $"{dotNet.ErrorMessage}", false);
        }

        var vcRedist = await new VCRedistInstaller().EnsureInstalledAsync(null, ct);
        if (!vcRedist.Success)
        {
            return new PreflightResult(false, null, null,
                $"The Visual C++ redistributable is required but could not be installed: " +
                $"{vcRedist.ErrorMessage}", false);
        }

        return new PreflightResult(true, pgDump, connectionString, null, false);
    }

    public async Task<bool> StopServiceAsync(CancellationToken ct) =>
        (await _service.StopAsync(ServiceTimeout, ct)).Success;

    public Task<BackupResult> BackupAsync(string pgDumpPath, string connectionString, CancellationToken ct) =>
        _backup.CreateAsync(pgDumpPath, connectionString, ct);

    public ConfigSnapshotHandle CaptureConfig()
    {
        var snapshot = ConfigSnapshot.Capture(ConfigPath);
        return new ConfigSnapshotHandle(snapshot.Restore);
    }

    public Task<bool> DeployAsync(CancellationToken ct) =>
        StoreHubPayload.ExtractAsync(config.StoreHubInstallPath, log, ct);

    public Task<MigrationRunResult> MigrateAsync(CancellationToken ct) =>
        MigrationRunner.RunAsync(config.StoreHubInstallPath, log, ct);

    public async Task<bool> StartServiceAsync(CancellationToken ct) =>
        (await _service.StartAsync(ServiceTimeout, ct)).Success;

    public Task<bool> HealthAsync(CancellationToken ct) =>
        HealthProbe.IsReadyAsync(config.HealthCheckPort, cancellationToken: ct);

    public string? ReadPosVersion() => PosAppVersion.Read(config.VelopackInstallPath);

    public async Task<bool> InstallPosAppAsync(CancellationToken ct) =>
        (await new VelopackLauncher().InstallAsync(config, null, ct)).Success;

    public void RestoreStoreHubTree(string stampDirectory) => _backup.RestoreStoreHubTree(stampDirectory);

    public Task WriteManifestAsync(CancellationToken ct) => InstallManifestWriter.WriteAsync(config, ct);
}
