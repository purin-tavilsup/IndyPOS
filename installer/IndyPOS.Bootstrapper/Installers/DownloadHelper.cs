namespace IndyPOS.Bootstrapper.Installers;

/// <summary>
/// Helper for downloading files with progress reporting.
/// </summary>
public static class DownloadHelper
{
    /// <summary>
    /// Download a file with progress reporting.
    /// </summary>
    public static async Task<string> DownloadFileAsync(
        string url,
        string destinationPath,
        IProgress<DownloadProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        using var client = new HttpClient();
        client.Timeout = TimeSpan.FromMinutes(30);

        using var response = await client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        response.EnsureSuccessStatusCode();

        var totalBytes = response.Content.Headers.ContentLength ?? -1;
        var fileName = Path.GetFileName(new Uri(url).LocalPath);

        progress?.Report(new DownloadProgress
        {
            StatusMessage = $"Downloading {fileName}...",
            Percentage = 0,
            BytesDownloaded = 0,
            TotalBytes = totalBytes
        });

        // Use unique temp file name to avoid conflicts
        var tempPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}_{fileName}");
        long bytesRead = 0;

        // Download to temp file - scope the streams so they're closed before move
        {
            await using var contentStream = await response.Content.ReadAsStreamAsync(cancellationToken);
            await using var fileStream = new FileStream(tempPath, FileMode.Create, FileAccess.Write, FileShare.None, 8192, true);

            var buffer = new byte[8192];
            int read;
            var lastReportTime = DateTime.UtcNow;

            while ((read = await contentStream.ReadAsync(buffer, cancellationToken)) > 0)
            {
                await fileStream.WriteAsync(buffer.AsMemory(0, read), cancellationToken);
                bytesRead += read;

                // Report progress every 100ms to avoid UI flooding
                if ((DateTime.UtcNow - lastReportTime).TotalMilliseconds > 100)
                {
                    var percentage = totalBytes > 0 ? (double)bytesRead / totalBytes : 0;
                    progress?.Report(new DownloadProgress
                    {
                        StatusMessage = $"Downloading {fileName}... {FormatBytes(bytesRead)} / {FormatBytes(totalBytes)}",
                        Percentage = percentage,
                        BytesDownloaded = bytesRead,
                        TotalBytes = totalBytes
                    });
                    lastReportTime = DateTime.UtcNow;
                }
            }
        } // Streams closed here

        progress?.Report(new DownloadProgress
        {
            StatusMessage = $"Downloaded {fileName}",
            Percentage = 1,
            BytesDownloaded = bytesRead,
            TotalBytes = bytesRead
        });

        // Move to destination if different
        if (tempPath != destinationPath)
        {
            if (File.Exists(destinationPath))
            {
                File.Delete(destinationPath);
            }
            File.Move(tempPath, destinationPath);
        }

        return destinationPath;
    }

    private static string FormatBytes(long bytes)
    {
        if (bytes < 0) return "unknown";
        if (bytes < 1024) return $"{bytes} B";
        if (bytes < 1024 * 1024) return $"{bytes / 1024.0:F1} KB";
        if (bytes < 1024 * 1024 * 1024) return $"{bytes / (1024.0 * 1024):F1} MB";
        return $"{bytes / (1024.0 * 1024 * 1024):F2} GB";
    }
}

/// <summary>
/// Progress information for downloads.
/// </summary>
public class DownloadProgress
{
    public required string StatusMessage { get; init; }
    public double Percentage { get; init; }
    public long BytesDownloaded { get; init; }
    public long TotalBytes { get; init; }
}
