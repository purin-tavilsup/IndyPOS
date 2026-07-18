using System.Runtime.InteropServices;

namespace IndyPOS.Bootstrapper.Silent;

/// <summary>
/// Best-effort attach to the launching console so Console output reaches the
/// caller. A no-op (returns false) when there is no parent console — e.g. a
/// PowerShell Direct host — in which case the durable log file is the record.
/// </summary>
internal static partial class ConsoleAttach
{
    private const int ATTACH_PARENT_PROCESS = -1;

    [LibraryImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool AttachConsole(int dwProcessId);

    public static bool TryAttach()
    {
        try { return AttachConsole(ATTACH_PARENT_PROCESS); }
        catch { return false; }
    }
}
