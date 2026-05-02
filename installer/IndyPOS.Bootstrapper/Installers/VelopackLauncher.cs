using System.Diagnostics;

namespace IndyPOS.Bootstrapper.Installers;

/// <summary>
/// Handles launching the Velopack installer for WinForms.
/// </summary>
public class VelopackLauncher
{
    private InstallationConfig? _config;

    private InstallationConfig Config =>
        _config ?? throw new InvalidOperationException(
            "VelopackLauncher has not been configured. Call InstallAsync first.");

    /// <summary>
    /// Install WinForms using Velopack Setup.exe.
    /// </summary>
    public async Task<VelopackLauncherResult> InstallAsync(
        InstallationConfig config,
        IProgress<int>? progress = null,
        CancellationToken cancellationToken = default)
    {
        _config = config;

        try
        {
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

            var psi = new ProcessStartInfo
            {
                FileName = setupPath,
                Arguments = "--silent",
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

    private string? FindSetupExecutable()
    {
        var setupFileName = $"{Config.VelopackAppId}-Setup.exe";

        var assembly = typeof(VelopackLauncher).Assembly;
        var resourceName = $"IndyPOS.Bootstrapper.Resources.{setupFileName}";

        using var resourceStream = assembly.GetManifestResourceStream(resourceName);

        if (resourceStream != null)
        {
            var tempPath = Path.Combine(Path.GetTempPath(), setupFileName);
            using var fileStream = File.Create(tempPath);
            resourceStream.CopyTo(fileStream);
            return tempPath;
        }

        var possiblePaths = new[]
        {
            Path.Combine(AppContext.BaseDirectory, setupFileName),
            Path.Combine(AppContext.BaseDirectory, "Setup.exe"),
            Path.Combine(AppContext.BaseDirectory, "Velopack", setupFileName),
            Path.Combine(AppContext.BaseDirectory, "WinForms", "Setup.exe")
        };

        return possiblePaths.FirstOrDefault(File.Exists);
    }

    private string GetWinFormsInstallPath()
    {
        return Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            Config.VelopackAppId,
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
