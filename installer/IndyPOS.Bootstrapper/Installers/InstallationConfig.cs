namespace IndyPOS.Bootstrapper.Installers;

/// <summary>
/// Configuration collected from the user for installation.
/// </summary>
public class InstallationConfig
{
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
