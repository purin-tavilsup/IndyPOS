using Serilog;
using System.Diagnostics.CodeAnalysis;
using Velopack;
using Velopack.Sources;

namespace IndyPOS.Windows.Forms.Services;

/// <summary>
/// Service for checking and applying Velopack updates from GitHub Releases.
/// </summary>
[ExcludeFromCodeCoverage]
public class UpdateService : IUpdateService
{
    // GitHub repository for updates
    private const string GitHubOwner = "ponggun";
    private const string GitHubRepo = "IndyPOS";

    private readonly UpdateManager _updateManager;
    private UpdateInfo? _pendingUpdate;

    public event EventHandler<UpdateAvailableEventArgs>? UpdateAvailable;

    public UpdateService()
    {
        var source = new GithubSource($"https://github.com/{GitHubOwner}/{GitHubRepo}", null, false);
        _updateManager = new UpdateManager(source);
    }

    /// <summary>
    /// Gets whether there is a pending update available.
    /// </summary>
    public bool HasPendingUpdate => _pendingUpdate != null;

    /// <summary>
    /// Gets the pending update version, if available.
    /// </summary>
    public string? PendingVersion => _pendingUpdate?.TargetFullRelease?.Version?.ToString();

    /// <summary>
    /// Checks for updates in the background. Call this on app startup.
    /// </summary>
    public async Task CheckForUpdatesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            Log.Information("Checking for updates...");

            _pendingUpdate = await _updateManager.CheckForUpdatesAsync();

            if (_pendingUpdate != null)
            {
                var version = _pendingUpdate.TargetFullRelease?.Version?.ToString() ?? "unknown";
                Log.Information("Update available: {Version}", version);

                UpdateAvailable?.Invoke(this, new UpdateAvailableEventArgs(version));
            }
            else
            {
                Log.Information("No updates available. App is up to date.");
            }
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Failed to check for updates");
            // Don't throw - update check failure should not block the app
        }
    }

    /// <summary>
    /// Downloads and applies the pending update, then restarts the application.
    /// </summary>
    public async Task ApplyUpdateAsync(Action<int>? progress = null, CancellationToken cancellationToken = default)
    {
        if (_pendingUpdate == null)
        {
            Log.Warning("No pending update to apply");
            return;
        }

        try
        {
            Log.Information("Downloading update: {Version}", PendingVersion);

            await _updateManager.DownloadUpdatesAsync(_pendingUpdate, progress);

            Log.Information("Download complete. Applying update and restarting...");

            _updateManager.ApplyUpdatesAndRestart(_pendingUpdate);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to apply update");
            throw;
        }
    }

    /// <summary>
    /// Downloads the update without restarting. Use this if you want to apply at a later time.
    /// </summary>
    public async Task DownloadUpdateAsync(Action<int>? progress = null, CancellationToken cancellationToken = default)
    {
        if (_pendingUpdate == null)
        {
            Log.Warning("No pending update to download");
            return;
        }

        try
        {
            Log.Information("Downloading update: {Version}", PendingVersion);

            await _updateManager.DownloadUpdatesAsync(_pendingUpdate, progress);

            Log.Information("Download complete. Update ready to apply.");
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to download update");
            throw;
        }
    }

    /// <summary>
    /// Applies a previously downloaded update when the app exits.
    /// Call this before Application.Exit().
    /// </summary>
    public void ApplyUpdateOnExit()
    {
        if (_pendingUpdate == null)
        {
            return;
        }

        try
        {
            Log.Information("Scheduling update to apply on exit: {Version}", PendingVersion);
            _updateManager.WaitExitThenApplyUpdates(_pendingUpdate);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to schedule update on exit");
        }
    }
}

/// <summary>
/// Interface for the update service.
/// </summary>
public interface IUpdateService
{
    /// <summary>
    /// Gets whether there is a pending update available.
    /// </summary>
    bool HasPendingUpdate { get; }

    /// <summary>
    /// Gets the pending update version, if available.
    /// </summary>
    string? PendingVersion { get; }

    /// <summary>
    /// Event raised when an update is available.
    /// </summary>
    event EventHandler<UpdateAvailableEventArgs>? UpdateAvailable;

    /// <summary>
    /// Checks for updates in the background.
    /// </summary>
    Task CheckForUpdatesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Downloads and applies the pending update, then restarts the application.
    /// </summary>
    Task ApplyUpdateAsync(Action<int>? progress = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Downloads the update without restarting.
    /// </summary>
    Task DownloadUpdateAsync(Action<int>? progress = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Applies a previously downloaded update when the app exits.
    /// </summary>
    void ApplyUpdateOnExit();
}

/// <summary>
/// Event args for update available notification.
/// </summary>
public class UpdateAvailableEventArgs(string version) : EventArgs
{
    public string Version { get; } = version;
}
