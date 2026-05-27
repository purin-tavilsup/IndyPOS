namespace IndyPOS.Bootstrapper.Installers;

/// <summary>
/// Snapshot of install metadata persisted at $SystemRoot\install-manifest.json
/// so teardown/verify scripts have a single source-of-truth for paths and
/// service identifiers — no need to mirror constants in PowerShell.
///
/// Sensitive values (passwords, JWT secret) are deliberately excluded; this
/// file is world-readable. Bump <see cref="ManifestVersion"/> on any
/// backwards-incompatible schema change.
/// </summary>
public record InstallManifest
{
    public int ManifestVersion { get; init; } = 1;
    public DateTime InstalledUtc { get; init; }
    public required string InstallVersion { get; init; }
    public required string SystemRoot { get; init; }
    public required string ConfigDirectory { get; init; }
    public required string KeysDirectory { get; init; }
    public required string LogsDirectory { get; init; }
    public required string BackupsDirectory { get; init; }
    public required string StoreHubInstallPath { get; init; }
    public required string ServiceName { get; init; }
    public required string ServiceDisplayName { get; init; }
    public required string VelopackAppId { get; init; }
    public required string VelopackInstallPath { get; init; }
    public required string DatabaseName { get; init; }
    public required string AppUser { get; init; }
    public required string PostgresBinPath { get; init; }
    public int HealthCheckPort { get; init; }

    public static InstallManifest From(InstallationConfig config) =>
        new()
        {
            InstalledUtc = DateTime.UtcNow,
            InstallVersion = config.InstallVersion,
            SystemRoot = config.SystemRoot,
            ConfigDirectory = config.ConfigDirectory,
            KeysDirectory = config.KeysDirectory,
            LogsDirectory = config.LogsDirectory,
            BackupsDirectory = config.BackupsDirectory,
            StoreHubInstallPath = config.StoreHubInstallPath,
            ServiceName = config.ServiceName,
            ServiceDisplayName = config.ServiceDisplayName,
            VelopackAppId = config.VelopackAppId,
            VelopackInstallPath = config.VelopackInstallPath,
            DatabaseName = config.DatabaseName,
            AppUser = config.AppUser,
            PostgresBinPath = config.PostgresBinPath,
            HealthCheckPort = config.HealthCheckPort
        };
}
