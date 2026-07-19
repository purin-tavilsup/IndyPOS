using System.Diagnostics;
// IndyPOS.Domain pulls in Prism.Core, whose buildTransitive targets add a global
// "using Prism.Dialogs;" (for ImplicitUsings consumers). Prism.Dialogs also declares
// a DialogResult type, ambiguous with the WinForms one used throughout this file.
using DialogResult = System.Windows.Forms.DialogResult;

namespace IndyPOS.Bootstrapper.Installers;

/// <summary>
/// Handles .NET Runtime detection and installation.
/// </summary>
public class DotNetInstaller
{
    // winget package id for the .NET 10 Desktop Runtime. winget ships with
    // Windows 11 and resolves the current point release for us, so we never
    // hardcode a build-specific download URL (which 404s every patch).
    private const string WingetPackageId = "Microsoft.DotNet.DesktopRuntime.10";

    // Fallback only: the official download page, opened in a browser when the
    // silent winget install isn't available (e.g. winget missing or offline).
    private const string DotNetDownloadPageUrl =
        "https://dotnet.microsoft.com/download/dotnet/10.0";

    // Upper bound on the silent winget install (download + install of ~55 MB).
    private static readonly TimeSpan WingetInstallTimeout = TimeSpan.FromMinutes(5);

    /// <summary>
    /// Ensure .NET 10 Runtime is installed. Tries a silent winget install
    /// first, then falls back to a guided manual install.
    /// </summary>
    public async Task<DotNetInstallerResult> EnsureInstalledAsync(
        IProgress<string>? log = null,
        CancellationToken cancellationToken = default,
        bool interactive = true)
    {
        log?.Report("Checking installed .NET runtimes...");

        if (IsDotNet10Installed())
        {
            log?.Report(".NET 10 Desktop Runtime is already installed");
            return new DotNetInstallerResult { Success = true, WasInstalled = false };
        }

        log?.Report(".NET 10 Desktop Runtime not found, installation required");

        if (await TryInstallViaWingetAsync(log, cancellationToken)
            && IsDotNet10Installed())
        {
            log?.Report(".NET 10 Desktop Runtime installed via winget");
            return new DotNetInstallerResult { Success = true, WasInstalled = true };
        }

        if (!interactive)
        {
            log?.Report(".NET 10 Desktop Runtime missing and could not be installed automatically (winget unavailable).");
            return new DotNetInstallerResult
            {
                Success = false,
                WasInstalled = false,
                ErrorMessage = ".NET 10 Desktop Runtime is required but is missing and winget could not install it automatically."
            };
        }

        log?.Report("Silent install unavailable; switching to manual install");
        return InstallManually(log);
    }

    /// <summary>
    /// Guided manual install: open the download page and poll until the user
    /// confirms .NET is installed. Fallback when winget can't be used.
    /// </summary>
    private DotNetInstallerResult InstallManually(IProgress<string>? log)
    {
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
    /// Attempt a silent winget install of the .NET 10 Desktop Runtime.
    /// Returns false (so the caller can fall back) on any failure — winget
    /// missing, offline, declined agreement, or a non-zero exit code.
    /// </summary>
    private static async Task<bool> TryInstallViaWingetAsync(
        IProgress<string>? log,
        CancellationToken cancellationToken)
    {
        log?.Report("Installing .NET 10 Desktop Runtime via winget...");

        var psi = new ProcessStartInfo
        {
            FileName = "winget",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };
        foreach (var arg in new[]
                 {
                     "install", "--exact", "--id", WingetPackageId,
                     "--source", "winget", "--silent",
                     "--accept-package-agreements", "--accept-source-agreements",
                     "--disable-interactivity"
                 })
        {
            psi.ArgumentList.Add(arg);
        }

        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(WingetInstallTimeout);

        try
        {
            using var process = Process.Start(psi);
            if (process is null)
            {
                return false;
            }

            // Drain both streams so a full pipe buffer can't deadlock winget.
            var stdout = process.StandardOutput.ReadToEndAsync(timeout.Token);
            var stderr = process.StandardError.ReadToEndAsync(timeout.Token);
            await process.WaitForExitAsync(timeout.Token);
            await Task.WhenAll(stdout, stderr);

            if (process.ExitCode != 0)
            {
                log?.Report($"winget exited with code {process.ExitCode}");
            }

            return process.ExitCode == 0;
        }
        catch (Exception ex)
        {
            log?.Report($"winget install could not run: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Check if .NET 10 Desktop Runtime is installed.
    /// </summary>
    private static bool IsDotNet10Installed()
    {
        var dotnetRoot = FindDotNetRoot();
        if (dotnetRoot is null)
        {
            return false;
        }

        // Prefer the filesystem probe: a versioned folder under
        // shared\Microsoft.WindowsDesktop.App is exactly what the runtime
        // needs to launch the app, and it requires no PATH or registry.
        return HasDesktopRuntime10Folder(dotnetRoot)
               || ListRuntimesReportsDesktop10(dotnetRoot);
    }

    /// <summary>
    /// Locate the dotnet install root without trusting the inherited PATH
    /// (which is captured at process start and goes stale if .NET was just
    /// installed by another tool, e.g. winget).
    /// </summary>
    private static string? FindDotNetRoot()
    {
        var candidates = new[]
        {
            Environment.GetEnvironmentVariable("DOTNET_ROOT"),
            Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
                "dotnet"),
            Environment.GetEnvironmentVariable("ProgramW6432") is { Length: > 0 } pf64
                ? Path.Combine(pf64, "dotnet")
                : null
        };

        return candidates.FirstOrDefault(
            path => !string.IsNullOrEmpty(path) && Directory.Exists(path));
    }

    /// <summary>
    /// True when a Microsoft.WindowsDesktop.App 10.x runtime folder exists.
    /// </summary>
    private static bool HasDesktopRuntime10Folder(string dotnetRoot)
    {
        var desktopApp = Path.Combine(
            dotnetRoot, "shared", "Microsoft.WindowsDesktop.App");

        if (!Directory.Exists(desktopApp))
        {
            return false;
        }

        return Directory.EnumerateDirectories(desktopApp)
            .Select(Path.GetFileName)
            .Any(name => name is not null
                         && name.StartsWith("10.", StringComparison.Ordinal));
    }

    /// <summary>
    /// Fallback: run the absolute dotnet.exe (not the bare PATH name) so a
    /// freshly-installed runtime is still seen by this already-running process.
    /// </summary>
    private static bool ListRuntimesReportsDesktop10(string dotnetRoot)
    {
        var dotnetExe = Path.Combine(dotnetRoot, "dotnet.exe");
        if (!File.Exists(dotnetExe))
        {
            return false;
        }

        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = dotnetExe,
                Arguments = "--list-runtimes",
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = Process.Start(psi);
            if (process is null)
            {
                return false;
            }

            var output = process.StandardOutput.ReadToEnd();
            process.WaitForExit(5000);

            return output.Contains("Microsoft.WindowsDesktop.App 10.");
        }
        catch
        {
            return false;
        }
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
