namespace IndyPOS.Bootstrapper.Silent;

public abstract record SilentOutcome;

public sealed record InstallSucceeded(
    bool AdminSeeded, string? CredFile, bool CredLocked, bool ServiceStarted, bool HealthOk) : SilentOutcome;

public sealed record InstallFailed(string Message) : SilentOutcome;

public sealed record InstallTimedOut : SilentOutcome;

public sealed record UsageErrorOutcome(string Message) : SilentOutcome;

public sealed record NotElevatedOutcome : SilentOutcome;

/// <summary>
/// Pure mapping from a run's outcome to an exit code + the non-secret marker
/// lines. No I/O — every permutation is unit-testable without running an install.
/// </summary>
public static class SilentOutcomeMapper
{
    private const string Prefix = "INDYPOS_MARKER ";
    private const string ResetHint =
        "run \"IndyPOS.StoreHub.exe reset-admin\" from the StoreHub install dir to reissue a bootstrap password";

    public static (int ExitCode, IReadOnlyList<string> Markers) Map(SilentOutcome outcome) => outcome switch
    {
        InstallSucceeded s => (0, SuccessMarkers(s)),
        InstallFailed => (2, [Prefix + "RESULT=failed"]),
        InstallTimedOut => (4, [Prefix + "RESULT=timeout"]),
        UsageErrorOutcome => (1, [Prefix + "RESULT=failed"]),
        NotElevatedOutcome => (3, [Prefix + "RESULT=failed"]),
        _ => throw new ArgumentOutOfRangeException(nameof(outcome))
    };

    private static IReadOnlyList<string> SuccessMarkers(InstallSucceeded s)
    {
        var markers = new List<string>
        {
            Prefix + "RESULT=success",
            Prefix + $"ADMIN_SEEDED={Lower(s.AdminSeeded)}"
        };

        if (s.AdminSeeded)
        {
            markers.Add(Prefix + $"CRED_FILE={s.CredFile}");
            markers.Add(Prefix + $"CRED_LOCKED={Lower(s.CredLocked)}");
        }
        else
        {
            markers.Add(Prefix + $"RESET_HINT={ResetHint}");
        }

        markers.Add(Prefix + $"SERVICE_STARTED={Lower(s.ServiceStarted)}");
        markers.Add(Prefix + $"HEALTH={(s.HealthOk ? "ok" : "failed")}");
        return markers;
    }

    private static string Lower(bool value) => value ? "true" : "false";
}
