namespace IndyPOS.Bootstrapper.Upgrade;

public enum UpgradeStage
{
    Preflight,
    Stop,
    Backup,
    Deploy,
    RestoreConfig,
    Migrate,
    Start,
    PosApp
}

/// <summary>
/// Runtime-selected fault injection for VM case 2 (forced mid-upgrade failure).
/// <para>Runtime, not compile-time, deliberately: at roughly 10 minutes per cycle plus a
/// 190 MB build, one binary serving both VM cases is the difference between one evening
/// and two. Undocumented in help output — it is a test hook, not a feature.</para>
/// </summary>
public static class SimulatedFailure
{
    public const string ArgumentName = "--simulate-failure";

    public static UpgradeStage? Parse(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var matched = Enum.GetNames<UpgradeStage>()
            .FirstOrDefault(n => n.Equals(value, StringComparison.OrdinalIgnoreCase));

        return matched is null ? null : Enum.Parse<UpgradeStage>(matched);
    }
}
