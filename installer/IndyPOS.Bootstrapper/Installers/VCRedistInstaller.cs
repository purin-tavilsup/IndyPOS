using System.Diagnostics;
using Microsoft.Win32;

namespace IndyPOS.Bootstrapper.Installers;

/// <summary>
/// Handles detection and silent install of the Visual C++ 2015-2022
/// Redistributable (x64), which PostgreSQL 18's initdb.exe depends on
/// (vcruntime140.dll / msvcp140.dll). Missing on clean Windows 11 boxes —
/// causes initdb to fail with exit code -1073741515 (STATUS_DLL_NOT_FOUND).
/// </summary>
public class VCRedistInstaller
{
    // aka.ms permalink — stable across VC++ Redistributable point releases.
    private const string VCRedistDownloadUrl =
        "https://aka.ms/vs/17/release/vc_redist.x64.exe";

    private const string InstallerFileName = "vc_redist.x64.exe";
    private const long MinValidInstallerSize = 10L * 1024 * 1024; // 10 MB; real file is ~25 MB

    private static readonly string CacheDirectory = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "IndyPOS.Bootstrapper",
        "cache");

    public async Task<VCRedistInstallerResult> EnsureInstalledAsync(
        IProgress<DownloadProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        if (IsInstalled())
        {
            progress?.Report(new DownloadProgress
            {
                StatusMessage = "Visual C++ Redistributable already installed",
                Percentage = 1
            });

            return new VCRedistInstallerResult { Success = true, WasInstalled = false };
        }

        Directory.CreateDirectory(CacheDirectory);
        var installerPath = Path.Combine(CacheDirectory, InstallerFileName);

        if (File.Exists(installerPath) && new FileInfo(installerPath).Length >= MinValidInstallerSize)
        {
            progress?.Report(new DownloadProgress
            {
                StatusMessage = $"Using cached VC++ Redistributable installer ({new FileInfo(installerPath).Length / 1024 / 1024} MB)",
                Percentage = 0.5
            });
        }
        else
        {
            progress?.Report(new DownloadProgress
            {
                StatusMessage = "Downloading Visual C++ Redistributable...",
                Percentage = 0
            });

            try
            {
                await DownloadHelper.DownloadFileAsync(
                    VCRedistDownloadUrl,
                    installerPath,
                    progress,
                    cancellationToken);
            }
            catch (HttpRequestException ex)
            {
                return new VCRedistInstallerResult
                {
                    Success = false,
                    ErrorMessage = $"Failed to download Visual C++ Redistributable: {ex.Message}\n\n" +
                                   "Please install it manually from:\n" +
                                   VCRedistDownloadUrl
                };
            }
        }

        progress?.Report(new DownloadProgress
        {
            StatusMessage = "Installing Visual C++ Redistributable...",
            Percentage = 0.7
        });

        var installResult = await RunSilentInstallAsync(installerPath, cancellationToken);
        if (!installResult.Success)
        {
            return installResult;
        }

        if (!IsInstalled())
        {
            return new VCRedistInstallerResult
            {
                Success = false,
                ErrorMessage = "Visual C++ Redistributable installation completed but could not be verified"
            };
        }

        progress?.Report(new DownloadProgress
        {
            StatusMessage = "Visual C++ Redistributable installed successfully",
            Percentage = 1
        });

        return new VCRedistInstallerResult { Success = true, WasInstalled = true };
    }

    private static async Task<VCRedistInstallerResult> RunSilentInstallAsync(
        string installerPath,
        CancellationToken cancellationToken)
    {
        var psi = new ProcessStartInfo
        {
            FileName = installerPath,
            Arguments = "/install /quiet /norestart",
            UseShellExecute = false,
            CreateNoWindow = true
        };

        try
        {
            using var process = Process.Start(psi);
            if (process == null)
            {
                return new VCRedistInstallerResult
                {
                    Success = false,
                    ErrorMessage = "Failed to start Visual C++ Redistributable installer"
                };
            }

            // VC++ redist install is fast (~30s) but allow generous slack for
            // slow disks / Defender real-time scanning.
            var timeoutMinutes = 5;
            var completed = await Task.Run(() =>
                process.WaitForExit((int)TimeSpan.FromMinutes(timeoutMinutes).TotalMilliseconds),
                cancellationToken);

            if (!completed)
            {
                try { process.Kill(); } catch { }
                return new VCRedistInstallerResult
                {
                    Success = false,
                    ErrorMessage = $"Visual C++ Redistributable installation timed out after {timeoutMinutes} minutes."
                };
            }

            // Exit code semantics (see VC++ Redist docs):
            //   0    — success
            //   1638 — newer version already installed; treat as success
            //   3010 — success but reboot required; tolerate (we don't reboot)
            var ec = process.ExitCode;
            if (ec == 0 || ec == 1638 || ec == 3010)
            {
                return new VCRedistInstallerResult { Success = true, WasInstalled = true };
            }

            return new VCRedistInstallerResult
            {
                Success = false,
                ErrorMessage = $"Visual C++ Redistributable installer exited with code {ec}."
            };
        }
        catch (Exception ex)
        {
            return new VCRedistInstallerResult
            {
                Success = false,
                ErrorMessage = $"Visual C++ Redistributable installation failed: {ex.Message}"
            };
        }
    }

    /// <summary>
    /// Detect VC++ 2015-2022 (x64) via the canonical Microsoft registry key.
    /// "14.0" covers the VC++ 14.x family (2015 → 2022); a fresh 2015+
    /// redistributable writes Installed=1 here.
    /// </summary>
    private static bool IsInstalled()
    {
        try
        {
            using var key = Registry.LocalMachine.OpenSubKey(
                @"SOFTWARE\Microsoft\VisualStudio\14.0\VC\Runtimes\x64");

            if (key?.GetValue("Installed") is int installed && installed == 1)
            {
                return true;
            }
        }
        catch
        {
            // Ignore registry errors
        }

        return false;
    }
}

public class VCRedistInstallerResult
{
    public bool Success { get; init; }
    public bool WasInstalled { get; init; }
    public string? ErrorMessage { get; init; }
}
