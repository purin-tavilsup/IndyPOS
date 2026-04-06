using Serilog;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.ServiceProcess;
using System.Text.Json;

namespace IndyPOS.Windows.Forms.Services;

/// <summary>
/// Service for checking and applying StoreHub service updates from GitHub Releases.
/// </summary>
[ExcludeFromCodeCoverage]
public class StoreHubUpdateService : IStoreHubUpdateService
{
    private const string GitHubOwner = "ponggun";
    private const string GitHubRepo = "IndyPOS";
    private const string ServiceName = "IndyPOS.StoreHub";
    private const string StoreHubPath = @"C:\Program Files\IndyPOS\StoreHub";
    private const string StoreHubHealthUrl = "http://localhost:5000/health";
    private const string StoreHubVersionUrl = "http://localhost:5000/version";

    private readonly HttpClient _httpClient;
    private string? _currentVersion;
    private string? _latestVersion;
    private string? _downloadUrl;

    public StoreHubUpdateService()
    {
        _httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
    }

    /// <summary>
    /// Gets whether there is a pending StoreHub update.
    /// </summary>
    public bool HasPendingUpdate => !string.IsNullOrEmpty(_latestVersion) &&
                                     !string.IsNullOrEmpty(_currentVersion) &&
                                     _latestVersion != _currentVersion;

    /// <summary>
    /// Gets the current StoreHub version.
    /// </summary>
    public string? CurrentVersion => _currentVersion;

    /// <summary>
    /// Gets the latest available version.
    /// </summary>
    public string? LatestVersion => _latestVersion;

    /// <summary>
    /// Event raised when an update is available.
    /// </summary>
    public event EventHandler<StoreHubUpdateEventArgs>? UpdateAvailable;

    /// <summary>
    /// Checks for StoreHub updates by comparing local version with GitHub releases.
    /// </summary>
    public async Task CheckForUpdatesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            Log.Information("Checking for StoreHub updates...");

            // Get current version from StoreHub
            _currentVersion = await GetCurrentVersionAsync(cancellationToken);
            if (string.IsNullOrEmpty(_currentVersion))
            {
                Log.Warning("Could not determine current StoreHub version");
                return;
            }

            // Get latest version from GitHub
            var (latestVersion, downloadUrl) = await GetLatestReleaseAsync(cancellationToken);
            _latestVersion = latestVersion;
            _downloadUrl = downloadUrl;

            if (HasPendingUpdate)
            {
                Log.Information("StoreHub update available: {CurrentVersion} -> {LatestVersion}",
                    _currentVersion, _latestVersion);

                UpdateAvailable?.Invoke(this, new StoreHubUpdateEventArgs(
                    _currentVersion, _latestVersion!));
            }
            else
            {
                Log.Information("StoreHub is up to date: {Version}", _currentVersion);
            }
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Failed to check for StoreHub updates");
        }
    }

    /// <summary>
    /// Applies the pending StoreHub update.
    /// This will stop the service, replace files, and restart it.
    /// </summary>
    public async Task ApplyUpdateAsync(IProgress<StoreHubUpdateProgress>? progress = null,
                                        CancellationToken cancellationToken = default)
    {
        if (!HasPendingUpdate || string.IsNullOrEmpty(_downloadUrl))
        {
            Log.Warning("No pending StoreHub update to apply");
            return;
        }

        try
        {
            progress?.Report(new StoreHubUpdateProgress("Stopping StoreHub service...", 10));
            await StopServiceAsync(cancellationToken);

            progress?.Report(new StoreHubUpdateProgress("Downloading update...", 30));
            var zipPath = await DownloadUpdateAsync(_downloadUrl, cancellationToken);

            progress?.Report(new StoreHubUpdateProgress("Backing up current version...", 50));
            await BackupCurrentVersionAsync(cancellationToken);

            progress?.Report(new StoreHubUpdateProgress("Installing update...", 70));
            await ExtractUpdateAsync(zipPath, cancellationToken);

            progress?.Report(new StoreHubUpdateProgress("Starting StoreHub service...", 90));
            await StartServiceAsync(cancellationToken);

            progress?.Report(new StoreHubUpdateProgress("Verifying health...", 95));
            await VerifyHealthAsync(cancellationToken);

            progress?.Report(new StoreHubUpdateProgress("Update complete!", 100));
            Log.Information("StoreHub update applied successfully: {Version}", _latestVersion);

            // Cleanup
            if (File.Exists(zipPath))
            {
                File.Delete(zipPath);
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to apply StoreHub update");
            throw;
        }
    }

    private async Task<string?> GetCurrentVersionAsync(CancellationToken cancellationToken)
    {
        try
        {
            var response = await _httpClient.GetStringAsync(StoreHubVersionUrl, cancellationToken);
            // Response might be JSON like {"version": "1.0.0"} or plain text
            if (response.StartsWith("{"))
            {
                using var doc = JsonDocument.Parse(response);
                return doc.RootElement.GetProperty("version").GetString();
            }
            return response.Trim();
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Could not get StoreHub version from API");

            // Try to read from assembly
            var exePath = Path.Combine(StoreHubPath, "IndyPOS.StoreHub.exe");
            if (File.Exists(exePath))
            {
                try
                {
                    var versionInfo = System.Diagnostics.FileVersionInfo.GetVersionInfo(exePath);
                    return versionInfo.FileVersion;
                }
                catch
                {
                    // Ignore
                }
            }

            return null;
        }
    }

    private async Task<(string? Version, string? DownloadUrl)> GetLatestReleaseAsync(
        CancellationToken cancellationToken)
    {
        try
        {
            var apiUrl = $"https://api.github.com/repos/{GitHubOwner}/{GitHubRepo}/releases/latest";

            using var request = new HttpRequestMessage(HttpMethod.Get, apiUrl);
            request.Headers.Add("User-Agent", "IndyPOS-UpdateService");

            var response = await _httpClient.SendAsync(request, cancellationToken);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            using var doc = JsonDocument.Parse(json);

            var tagName = doc.RootElement.GetProperty("tag_name").GetString();
            var version = tagName?.TrimStart('v');

            // Find the StoreHub zip asset
            string? downloadUrl = null;
            if (doc.RootElement.TryGetProperty("assets", out var assets))
            {
                foreach (var asset in assets.EnumerateArray())
                {
                    var name = asset.GetProperty("name").GetString();
                    if (name != null && name.Contains("StoreHub", StringComparison.OrdinalIgnoreCase) &&
                        name.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
                    {
                        downloadUrl = asset.GetProperty("browser_download_url").GetString();
                        break;
                    }
                }
            }

            return (version, downloadUrl);
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Failed to get latest release from GitHub");
            return (null, null);
        }
    }

    private async Task StopServiceAsync(CancellationToken cancellationToken)
    {
        try
        {
            using var sc = new ServiceController(ServiceName);
            if (sc.Status == ServiceControllerStatus.Running)
            {
                Log.Information("Stopping StoreHub service...");
                sc.Stop();
                sc.WaitForStatus(ServiceControllerStatus.Stopped, TimeSpan.FromSeconds(30));
                Log.Information("StoreHub service stopped");
            }
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("not found"))
        {
            Log.Warning("StoreHub service not found");
        }
    }

    private async Task StartServiceAsync(CancellationToken cancellationToken)
    {
        try
        {
            using var sc = new ServiceController(ServiceName);
            if (sc.Status != ServiceControllerStatus.Running)
            {
                Log.Information("Starting StoreHub service...");
                sc.Start();
                sc.WaitForStatus(ServiceControllerStatus.Running, TimeSpan.FromSeconds(30));
                Log.Information("StoreHub service started");
            }
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("not found"))
        {
            Log.Error("StoreHub service not found - cannot start");
            throw;
        }
    }

    private async Task<string> DownloadUpdateAsync(string downloadUrl, CancellationToken cancellationToken)
    {
        var tempPath = Path.Combine(Path.GetTempPath(), $"StoreHub-update-{Guid.NewGuid()}.zip");

        using var response = await _httpClient.GetAsync(downloadUrl, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        response.EnsureSuccessStatusCode();

        await using var fileStream = new FileStream(tempPath, FileMode.Create, FileAccess.Write, FileShare.None);
        await response.Content.CopyToAsync(fileStream, cancellationToken);

        return tempPath;
    }

    private async Task BackupCurrentVersionAsync(CancellationToken cancellationToken)
    {
        var backupPath = $"{StoreHubPath}.backup";

        if (Directory.Exists(backupPath))
        {
            Directory.Delete(backupPath, recursive: true);
        }

        if (Directory.Exists(StoreHubPath))
        {
            // Copy instead of move to avoid issues with locked files
            CopyDirectory(StoreHubPath, backupPath);
            Log.Information("Backed up current StoreHub to {BackupPath}", backupPath);
        }
    }

    private async Task ExtractUpdateAsync(string zipPath, CancellationToken cancellationToken)
    {
        // Delete existing files (except config)
        if (Directory.Exists(StoreHubPath))
        {
            foreach (var file in Directory.GetFiles(StoreHubPath, "*", SearchOption.AllDirectories))
            {
                // Preserve configuration files
                if (file.EndsWith("appsettings.json", StringComparison.OrdinalIgnoreCase) ||
                    file.EndsWith("appsettings.Production.json", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                try
                {
                    File.Delete(file);
                }
                catch (Exception ex)
                {
                    Log.Warning(ex, "Could not delete file: {File}", file);
                }
            }
        }
        else
        {
            Directory.CreateDirectory(StoreHubPath);
        }

        // Extract new files
        ZipFile.ExtractToDirectory(zipPath, StoreHubPath, overwriteFiles: true);
        Log.Information("Extracted StoreHub update to {Path}", StoreHubPath);
    }

    private async Task VerifyHealthAsync(CancellationToken cancellationToken)
    {
        // Wait a moment for service to initialize
        await Task.Delay(2000, cancellationToken);

        for (int i = 0; i < 5; i++)
        {
            try
            {
                var response = await _httpClient.GetAsync(StoreHubHealthUrl, cancellationToken);
                if (response.IsSuccessStatusCode)
                {
                    Log.Information("StoreHub health check passed");
                    return;
                }
            }
            catch
            {
                // Retry
            }

            await Task.Delay(1000, cancellationToken);
        }

        Log.Warning("StoreHub health check did not pass after update");
    }

    private static void CopyDirectory(string sourceDir, string destDir)
    {
        Directory.CreateDirectory(destDir);

        foreach (var file in Directory.GetFiles(sourceDir))
        {
            var destFile = Path.Combine(destDir, Path.GetFileName(file));
            File.Copy(file, destFile, overwrite: true);
        }

        foreach (var dir in Directory.GetDirectories(sourceDir))
        {
            var destSubDir = Path.Combine(destDir, Path.GetFileName(dir));
            CopyDirectory(dir, destSubDir);
        }
    }
}

/// <summary>
/// Interface for StoreHub update service.
/// </summary>
public interface IStoreHubUpdateService
{
    bool HasPendingUpdate { get; }
    string? CurrentVersion { get; }
    string? LatestVersion { get; }
    event EventHandler<StoreHubUpdateEventArgs>? UpdateAvailable;
    Task CheckForUpdatesAsync(CancellationToken cancellationToken = default);
    Task ApplyUpdateAsync(IProgress<StoreHubUpdateProgress>? progress = null, CancellationToken cancellationToken = default);
}

/// <summary>
/// Event args for StoreHub update notifications.
/// </summary>
public class StoreHubUpdateEventArgs(string currentVersion, string newVersion) : EventArgs
{
    public string CurrentVersion { get; } = currentVersion;
    public string NewVersion { get; } = newVersion;
}

/// <summary>
/// Progress information for StoreHub updates.
/// </summary>
public class StoreHubUpdateProgress(string statusMessage, int percentage)
{
    public string StatusMessage { get; } = statusMessage;
    public int Percentage { get; } = percentage;
}
