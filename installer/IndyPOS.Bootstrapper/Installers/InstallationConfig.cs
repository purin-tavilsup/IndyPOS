using System.Reflection;

namespace IndyPOS.Bootstrapper.Installers;

/// <summary>
/// Configuration collected from the user for installation, plus
/// computed paths/identifiers that are derived from the install version.
/// </summary>
public class InstallationConfig
{
    private static readonly string DefaultInstallVersion = ResolveAssemblyVersion();

    /// <summary>
    /// Unique identifier for this store (e.g., "STORE-001").
    /// </summary>
    public required string StoreId { get; init; }

    /// <summary>
    /// Password for the database application user.
    /// </summary>
    public required string AppPassword { get; init; }

    /// <summary>
    /// PostgreSQL bin directory (auto-detected after install).
    /// </summary>
    public string PostgresBinPath { get; set; } = @"C:\Program Files\PostgreSQL\18\bin";

    /// <summary>
    /// Database name to create.
    /// </summary>
    public string DatabaseName { get; init; } = "indypos_storehub";

    /// <summary>
    /// Database application user name.
    /// </summary>
    public string AppUser { get; init; } = "indypos_app";

    /// <summary>
    /// Version this installer targets (e.g., "4.0.0"). Defaults to the
    /// bootstrapper assembly version so paths and IDs stay in lockstep.
    /// </summary>
    public string InstallVersion { get; init; } = DefaultInstallVersion;

    /// <summary>
    /// Root directory for all v4 system-shared state. v3.7.0 lives next to
    /// this under C:\ProgramData\IndyPOS\ but never inside this folder.
    /// </summary>
    public string SystemRoot => Path.Combine(@"C:\ProgramData\IndyPOS", $"v{InstallVersion}");

    public string ConfigDirectory => Path.Combine(SystemRoot, "Config");
    public string KeysDirectory => Path.Combine(SystemRoot, "keys");
    public string LogsDirectory => Path.Combine(SystemRoot, "logs");
    public string BackupsDirectory => Path.Combine(SystemRoot, "backups");
    public string StoreHubInstallPath => Path.Combine(SystemRoot, "StoreHub");

    public string ServiceName => $"IndyPOS.StoreHub.v{Major}";
    public string ServiceDisplayName => $"IndyPOS StoreHub v{Major}";
    public string ServiceDescription => "IndyPOS local API service for point-of-sale operations";

    public string VelopackAppId => $"IndyPOS.POS.v{Major}";

    public int HealthCheckPort => 5000;

    private string Major => InstallVersion.Split('.')[0];

    private static string ResolveAssemblyVersion()
    {
        var version = typeof(InstallationConfig).Assembly.GetName().Version;
        return version is null
            ? "4.0.0"
            : $"{version.Major}.{version.Minor}.{version.Build}";
    }
}

/// <summary>
/// Progress update from the installation process.
/// </summary>
public class InstallationProgress
{
    public required string StepName { get; init; }
    public required string StatusMessage { get; init; }
    public int Percentage { get; init; } = -1; // -1 for indeterminate
    public string? LogMessage { get; init; }
    public bool IsError { get; init; }

    public static InstallationProgress Step(string stepName, string status, int percentage = -1) =>
        new() { StepName = stepName, StatusMessage = status, Percentage = percentage };

    public static InstallationProgress Log(string message) =>
        new() { StepName = "", StatusMessage = "", LogMessage = message };

    public static InstallationProgress Error(string message) =>
        new() { StepName = "", StatusMessage = "", LogMessage = message, IsError = true };
}
