using IndyPOS.Application.Common;

namespace IndyPOS.Windows.Forms;

/// <summary>Where the till writes its log file.</summary>
public static class TillLogDirectory
{
    /// <summary>Read before configuration exists, so it is a plain environment variable.</summary>
    public const string Variable = "IndyPOS_TillLogsDirectory";

    /// <summary>The installed logs directory, unless a dev run names its own (each dev store's till).</summary>
    public static string Resolve(string? overrideDirectory) =>
        string.IsNullOrWhiteSpace(overrideDirectory) ? InstallPaths.LogsDirectory : overrideDirectory;
}
