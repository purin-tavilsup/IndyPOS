using System.Diagnostics;
using System.IO.Compression;

namespace IndyPOS.Bootstrapper.Installers;

/// <summary>
/// Handles launching the Velopack installer for WinForms.
/// </summary>
public class VelopackLauncher
{
    /// <summary>
    /// Install WinForms using Velopack Setup.exe.
    /// </summary>
    public async Task<VelopackLauncherResult> InstallAsync(
        IProgress<int>? progress = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Look for Velopack Setup.exe
            var setupPath = FindSetupExecutable();

            if (string.IsNullOrEmpty(setupPath))
            {
                return new VelopackLauncherResult
                {
                    Success = false,
                    ErrorMessage = "Velopack Setup.exe not found. WinForms installation skipped."
                };
            }

            progress?.Report(10);

            // Run Setup.exe with silent/minimal UI
            var psi = new ProcessStartInfo
            {
                FileName = setupPath,
                Arguments = "--silent", // Or use --minimized for minimal UI
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = Process.Start(psi);
            if (process == null)
            {
                return new VelopackLauncherResult
                {
                    Success = false,
                    ErrorMessage = "Failed to start Velopack Setup.exe"
                };
            }

            progress?.Report(50);

            // Wait for installation to complete (up to 5 minutes)
            var completed = await Task.Run(() =>
                process.WaitForExit((int)TimeSpan.FromMinutes(5).TotalMilliseconds),
                cancellationToken);

            if (!completed)
            {
                try { process.Kill(); } catch { }
                return new VelopackLauncherResult
                {
                    Success = false,
                    ErrorMessage = "Velopack installation timed out"
                };
            }

            progress?.Report(90);

            // Velopack Setup.exe returns 0 on success
            if (process.ExitCode != 0)
            {
                return new VelopackLauncherResult
                {
                    Success = false,
                    ErrorMessage = $"Velopack Setup.exe exited with code {process.ExitCode}"
                };
            }

            progress?.Report(100);

            return new VelopackLauncherResult
            {
                Success = true,
                InstallPath = GetWinFormsInstallPath()
            };
        }
        catch (Exception ex)
        {
            return new VelopackLauncherResult
            {
                Success = false,
                ErrorMessage = ex.Message
            };
        }
    }

    /// <summary>
    /// Find the Velopack Setup.exe file.
    /// </summary>
    private static string? FindSetupExecutable()
    {
        // Check for embedded resource
        var assembly = typeof(VelopackLauncher).Assembly;
        var resourceName = "IndyPOS.Bootstrapper.Resources.IndyPOS.POS-Setup.exe";

        using var resourceStream = assembly.GetManifestResourceStream(resourceName);

        if (resourceStream != null)
        {
            // Extract to temp location
            var tempPath = Path.Combine(Path.GetTempPath(), "IndyPOS.POS-Setup.exe");
            using var fileStream = File.Create(tempPath);
            resourceStream.CopyTo(fileStream);
            return tempPath;
        }

        // Check for external file locations
        var possiblePaths = new[]
        {
            Path.Combine(AppContext.BaseDirectory, "IndyPOS.POS-Setup.exe"),
            Path.Combine(AppContext.BaseDirectory, "Setup.exe"),
            Path.Combine(AppContext.BaseDirectory, "Velopack", "IndyPOS.POS-Setup.exe"),
            Path.Combine(AppContext.BaseDirectory, "WinForms", "Setup.exe")
        };

        return possiblePaths.FirstOrDefault(File.Exists);
    }

    /// <summary>
    /// Get the installation path for WinForms (Velopack default location).
    /// </summary>
    private static string GetWinFormsInstallPath()
    {
        return Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "IndyPOS.POS",
            "current");
    }
}

/// <summary>
/// Result of Velopack launcher.
/// </summary>
public class VelopackLauncherResult
{
    public bool Success { get; init; }
    public string InstallPath { get; init; } = "";
    public string? ErrorMessage { get; init; }
}
