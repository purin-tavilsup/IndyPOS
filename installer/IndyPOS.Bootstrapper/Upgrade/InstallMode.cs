using IndyPOS.Domain.Enums;

namespace IndyPOS.Bootstrapper.Upgrade;

/// <summary>
/// How this machine should be treated. Three outcomes, not two: a machine can be
/// neither freshly installable nor upgradeable, and treating that as Fresh sends the
/// run past the point where it starts mutating the install.
/// </summary>
public enum InstallMode
{
    Fresh,
    Upgrade,
    Unusable
}

/// <param name="InstalledVersion">From install-manifest.json. Null unless a manifest was found.</param>
/// <param name="StoreId">From appsettings.json — the manifest does not record it.</param>
/// <param name="Reason">Operator-facing explanation. Surfaced on Unusable.</param>
public sealed record DetectedInstall(
    InstallMode Mode,
    string? InstalledVersion,
    string? StoreId,
    StoreType? StoreType,
    string Reason);
