using System.Security.Principal;

namespace IndyPOS.Bootstrapper.Silent;

/// <summary>Shared elevation check for the interactive and silent entry paths.</summary>
public static class Elevation
{
    public static bool IsElevated()
    {
        using var identity = WindowsIdentity.GetCurrent();
        return new WindowsPrincipal(identity).IsInRole(WindowsBuiltInRole.Administrator);
    }
}
