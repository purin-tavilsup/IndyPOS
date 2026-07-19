using System.Reflection;
using System.Security.Cryptography;

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
    /// Type of store, determines which features (e.g. PayLater, payment methods)
    /// are available. Chosen once, at install time; immutable afterward.
    /// </summary>
    public IndyPOS.Domain.Enums.StoreType StoreType { get; init; } = IndyPOS.Domain.Enums.StoreType.GeneralHardware;

    /// <summary>
    /// True for the interactive wizard; false for the headless --silent path.
    /// When false, prerequisite installers must never block on UI (e.g. the
    /// .NET manual-install dialog) — they fail fast so the run can't hang.
    /// </summary>
    public bool Interactive { get; init; } = true;

    /// <summary>
    /// Username for the initial SystemAdmin login (chosen in the wizard).
    /// </summary>
    public string AdminUsername { get; init; } = "admin";

    /// <summary>
    /// Password for the initial SystemAdmin login. Generated (not user-chosen):
    /// a random, single-use BOOTSTRAP credential shown once on the finish screen
    /// and force-rotated on first login. Evaluated once so the seeded value and
    /// the displayed value never diverge.
    /// </summary>
    public string AdminPassword { get; init; } = GenerateAdminPassword();

    /// <summary>
    /// Password for the local database application user. This is a
    /// machine-to-machine secret (Postgres listens only on 127.0.0.1), so it
    /// is auto-generated rather than chosen — the person installing never needs
    /// to know it. Alphanumeric to stay safe in connection strings and SQL.
    /// </summary>
    public string AppPassword { get; init; } = GenerateSecret();

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
    /// Root directory for all v4 system-shared state. Keyed off the MAJOR
    /// version (e.g. "v4") so it stays stable across patch updates and lines
    /// up with <see cref="ServiceName"/> / <see cref="VelopackAppId"/>. v3.7.0
    /// lives next to this under C:\ProgramData\IndyPOS\ but never inside it.
    /// </summary>
    public string SystemRoot => Path.Combine(@"C:\ProgramData\IndyPOS", $"v{Major}");

    public string ConfigDirectory => Path.Combine(SystemRoot, "Config");
    public string KeysDirectory => Path.Combine(SystemRoot, "keys");
    public string LogsDirectory => Path.Combine(SystemRoot, "logs");
    public string BackupsDirectory => Path.Combine(SystemRoot, "backups");
    public string StoreHubInstallPath => Path.Combine(SystemRoot, "StoreHub");

    public string ServiceName => $"IndyPOS.StoreHub.v{Major}";
    public string ServiceDisplayName => $"IndyPOS StoreHub v{Major}";
    public string ServiceDescription => "IndyPOS local API service for point-of-sale operations";

    public string VelopackAppId => $"IndyPOS.POS.v{Major}";

    /// <summary>
    /// Where Velopack drops the WinForms install (per-user, supports auto-update).
    /// Single source-of-truth for both <see cref="VelopackLauncher"/> and the
    /// install manifest.
    /// </summary>
    public string VelopackInstallPath => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        VelopackAppId,
        "current");

    public int HealthCheckPort => 5000;

    private string Major => InstallVersion.Split('.')[0];

    private static string ResolveAssemblyVersion()
    {
        var version = typeof(InstallationConfig).Assembly.GetName().Version;
        return version is null
            ? "4.0.0"
            : $"{version.Major}.{version.Minor}.{version.Build}";
    }

    // 32 alphanumeric chars (~190 bits). Alphanumeric avoids any quoting/escaping
    // hazard in the Npgsql connection string and the CREATE/ALTER ROLE SQL.
    private static string GenerateSecret() =>
        GenerateRandomString("ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789", 32);

    // 14 chars from a 57-char alphabet with ambiguous glyphs (0/O/1/l/I) removed
    // so it is easy to read off the finish screen and type once. Single-use.
    private static string GenerateAdminPassword() =>
        GenerateRandomString("ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnopqrstuvwxyz23456789", 14);

    // Cryptographically-random string drawn uniformly from the given alphabet.
    // RandomNumberGenerator.GetInt32 is unbiased, so no modulo skew.
    private static string GenerateRandomString(string alphabet, int length)
    {
        var chars = new char[length];
        for (var i = 0; i < chars.Length; i++)
        {
            chars[i] = alphabet[RandomNumberGenerator.GetInt32(alphabet.Length)];
        }
        return new string(chars);
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
