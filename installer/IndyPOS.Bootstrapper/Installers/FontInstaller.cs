using System.Drawing.Text;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using Microsoft.Win32;

namespace IndyPOS.Bootstrapper.Installers;

/// <summary>
/// Installs the bundled FC Subject UI fonts system-wide so the WinForms app —
/// which references the family "FC Subject [Non-commercial] Reg" by name — renders
/// as designed instead of falling back to a substitute face. Fonts are embedded
/// resources under "…Resources.Fonts.*.ttf" (staged by build-installer.ps1).
/// Idempotent and best-effort: failures are reported, never thrown.
/// </summary>
[SupportedOSPlatform("windows")]
public class FontInstaller
{
    private const string FontsRegistryKey = @"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Fonts";
    private const string ResourceMarker = ".Resources.Fonts.";

    private const int WM_FONTCHANGE = 0x001D;
    private const int HWND_BROADCAST = 0xFFFF;
    private const uint SMTO_ABORTIFHUNG = 0x0002;

    [DllImport("gdi32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern int AddFontResource(string lpFileName);

    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    private static extern IntPtr SendMessageTimeout(
        IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam, uint flags, uint timeout, out IntPtr result);

    /// <summary>The Windows font registry entry name for a TrueType family.</summary>
    internal static string BuildFontRegistryName(string familyName) => $"{familyName} (TrueType)";

    /// <summary>
    /// Extracts and installs every bundled .ttf font. Returns counts; never throws.
    /// </summary>
    public FontInstallerResult Install(IProgress<string>? log = null)
    {
        try
        {
            var assembly = typeof(FontInstaller).Assembly;
            var resources = assembly.GetManifestResourceNames()
                .Where(n => n.Contains(ResourceMarker, StringComparison.OrdinalIgnoreCase)
                         && n.EndsWith(".ttf", StringComparison.OrdinalIgnoreCase))
                .ToArray();

            if (resources.Length == 0)
            {
                log?.Report("No bundled fonts found; skipping font installation.");
                return new FontInstallerResult { Success = true };
            }

            var fontsDir = Environment.GetFolderPath(Environment.SpecialFolder.Fonts);
            using var regKey = Registry.LocalMachine.OpenSubKey(FontsRegistryKey, writable: true);
            if (regKey is null)
            {
                return new FontInstallerResult
                {
                    Success = false,
                    ErrorMessage = $"Could not open font registry key HKLM\\{FontsRegistryKey}."
                };
            }

            var installed = 0;
            var skipped = 0;
            var changed = false;

            foreach (var resource in resources)
            {
                var fileName = resource[(resource.IndexOf(ResourceMarker, StringComparison.OrdinalIgnoreCase)
                                         + ResourceMarker.Length)..];
                var destPath = Path.Combine(fontsDir, fileName);

                var familyName = ReadFamilyName(assembly, resource);
                if (familyName is null)
                {
                    log?.Report($"Skipping '{fileName}': could not read font family name.");
                    continue;
                }

                var regName = BuildFontRegistryName(familyName);

                if (regKey.GetValue(regName) is not null && File.Exists(destPath))
                {
                    log?.Report($"Font already installed: {familyName}");
                    skipped++;
                    continue;
                }

                ExtractResource(assembly, resource, destPath);
                AddFontResource(destPath);
                regKey.SetValue(regName, fileName);
                changed = true;
                installed++;
                log?.Report($"Installed font: {familyName}");
            }

            if (changed)
            {
                // Make the new fonts available to already-running/next processes
                // without a reboot. ABORTIFHUNG + timeout so a stuck top-level
                // window can't hang the installer.
                SendMessageTimeout((IntPtr)HWND_BROADCAST, WM_FONTCHANGE, IntPtr.Zero, IntPtr.Zero,
                    SMTO_ABORTIFHUNG, 1000, out _);
            }

            return new FontInstallerResult { Success = true, Installed = installed, Skipped = skipped };
        }
        catch (Exception ex)
        {
            return new FontInstallerResult { Success = false, ErrorMessage = ex.Message };
        }
    }

    private static string? ReadFamilyName(Assembly assembly, string resource)
    {
        try
        {
            using var stream = assembly.GetManifestResourceStream(resource);
            if (stream is null)
            {
                return null;
            }

            using var ms = new MemoryStream();
            stream.CopyTo(ms);
            var bytes = ms.ToArray();

            using var pfc = new PrivateFontCollection();
            var handle = GCHandle.Alloc(bytes, GCHandleType.Pinned);
            try
            {
                pfc.AddMemoryFont(handle.AddrOfPinnedObject(), bytes.Length);
            }
            finally
            {
                handle.Free();
            }

            return pfc.Families.Length > 0 ? pfc.Families[0].Name : null;
        }
        catch
        {
            return null;
        }
    }

    private static void ExtractResource(Assembly assembly, string resource, string destPath)
    {
        using var stream = assembly.GetManifestResourceStream(resource)
            ?? throw new InvalidOperationException($"Embedded font resource missing: {resource}");
        using var file = File.Create(destPath);
        stream.CopyTo(file);
    }
}

/// <summary>Result of font installation.</summary>
public class FontInstallerResult
{
    public bool Success { get; init; }
    public int Installed { get; init; }
    public int Skipped { get; init; }
    public string? ErrorMessage { get; init; }
}
