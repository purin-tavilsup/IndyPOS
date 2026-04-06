using System.Diagnostics;
using Microsoft.Win32;

namespace IndyPOS.Bootstrapper.Installers;

/// <summary>
/// Handles .NET Runtime detection and installation.
/// </summary>
public class DotNetInstaller
{
    // .NET 10 Desktop Runtime download URL (update when final release is available)
    private const string DotNetDownloadUrl =
        "https://download.visualstudio.microsoft.com/download/pr/dotnet-runtime-10.0-win-x64.exe";

    // Alternative: Use the official download page redirect
    private const string DotNetDownloadPageUrl =
        "https://dotnet.microsoft.com/download/dotnet/10.0";

    /// <summary>
    /// Ensure .NET 10 Runtime is installed.
    /// </summary>
    public async Task<DotNetInstallerResult> EnsureInstalledAsync(
        IProgress<string>? log = null,
        CancellationToken cancellationToken = default)
    {
        log?.Report("Checking installed .NET runtimes...");

        if (IsDotNet10Installed())
        {
            log?.Report(".NET 10 Desktop Runtime is already installed");
            return new DotNetInstallerResult { Success = true, WasInstalled = false };
        }

        log?.Report(".NET 10 Desktop Runtime not found, installation required");

        // For now, prompt user to install manually since .NET 10 URLs may change
        // In production, we'd download and run the installer silently

        var message = ".NET 10 Desktop Runtime is required but not installed.\n\n" +
                      "Please download and install it from:\n" +
                      "https://dotnet.microsoft.com/download/dotnet/10.0\n\n" +
                      "Choose 'Download .NET Desktop Runtime' for Windows x64.";

        var result = MessageBox.Show(
            message,
            ".NET Runtime Required",
            MessageBoxButtons.OKCancel,
            MessageBoxIcon.Information);

        if (result == DialogResult.Cancel)
        {
            return new DotNetInstallerResult
            {
                Success = false,
                WasInstalled = false,
                ErrorMessage = "User cancelled .NET installation"
            };
        }

        // Open download page
        Process.Start(new ProcessStartInfo
        {
            FileName = DotNetDownloadPageUrl,
            UseShellExecute = true
        });

        // Wait for user to complete installation
        var waitMessage = "Please complete the .NET 10 installation and click OK when done.\n\n" +
                          "Click Cancel to abort the IndyPOS installation.";

        while (true)
        {
            var waitResult = MessageBox.Show(
                waitMessage,
                "Waiting for .NET Installation",
                MessageBoxButtons.OKCancel,
                MessageBoxIcon.Question);

            if (waitResult == DialogResult.Cancel)
            {
                return new DotNetInstallerResult
                {
                    Success = false,
                    WasInstalled = false,
                    ErrorMessage = "User cancelled .NET installation"
                };
            }

            // Re-check if .NET is now installed
            if (IsDotNet10Installed())
            {
                log?.Report(".NET 10 Desktop Runtime installation confirmed");
                return new DotNetInstallerResult { Success = true, WasInstalled = true };
            }

            var retryMessage = ".NET 10 Desktop Runtime is still not detected.\n\n" +
                               "Please ensure you installed the Desktop Runtime (not just SDK).\n\n" +
                               "Click Retry to check again, or Cancel to abort.";

            var retryResult = MessageBox.Show(
                retryMessage,
                ".NET Not Detected",
                MessageBoxButtons.RetryCancel,
                MessageBoxIcon.Warning);

            if (retryResult == DialogResult.Cancel)
            {
                return new DotNetInstallerResult
                {
                    Success = false,
                    WasInstalled = false,
                    ErrorMessage = ".NET 10 Desktop Runtime not detected after installation"
                };
            }
        }
    }

    /// <summary>
    /// Check if .NET 10 Desktop Runtime is installed.
    /// </summary>
    private static bool IsDotNet10Installed()
    {
        // Method 1: Check via registry
        try
        {
            using var key = Registry.LocalMachine.OpenSubKey(
                @"SOFTWARE\dotnet\Setup\InstalledVersions\x64\sharedfx\Microsoft.WindowsDesktop.App");

            if (key != null)
            {
                var versions = key.GetSubKeyNames();
                if (versions.Any(v => v.StartsWith("10.")))
                {
                    return true;
                }
            }
        }
        catch
        {
            // Ignore registry errors
        }

        // Method 2: Check via dotnet --list-runtimes
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = "dotnet",
                Arguments = "--list-runtimes",
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = Process.Start(psi);
            if (process != null)
            {
                var output = process.StandardOutput.ReadToEnd();
                process.WaitForExit(5000);

                // Look for Microsoft.WindowsDesktop.App 10.x.x
                return output.Contains("Microsoft.WindowsDesktop.App 10.");
            }
        }
        catch
        {
            // dotnet command not found or failed
        }

        return false;
    }
}

/// <summary>
/// Result of .NET installation check/install.
/// </summary>
public class DotNetInstallerResult
{
    public bool Success { get; init; }
    public bool WasInstalled { get; init; }
    public string? ErrorMessage { get; init; }
}
