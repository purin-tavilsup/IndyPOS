using FluentAssertions;
using IndyPOS.Bootstrapper.Installers;

namespace IndyPOS.Bootstrapper.Tests.Installers;

public class DownloadHelperTests
{
    [Fact]
    public async Task DownloadFileAsync_WithValidUrl_ShouldDownloadFile()
    {
        // Arrange
        // Using a small, reliable file for testing
        var url = "https://www.google.com/robots.txt";
        var destPath = Path.Combine(Path.GetTempPath(), $"test-download-{Guid.NewGuid()}.txt");
        var progressReports = new List<DownloadProgress>();
        var progress = new Progress<DownloadProgress>(p => progressReports.Add(p));

        try
        {
            // Act
            var result = await DownloadHelper.DownloadFileAsync(url, destPath, progress);

            // Assert
            result.Should().Be(destPath);
            File.Exists(destPath).Should().BeTrue();
            var content = await File.ReadAllTextAsync(destPath);
            content.Should().Contain("User-agent"); // robots.txt contains this
        }
        finally
        {
            // Cleanup
            if (File.Exists(destPath))
            {
                File.Delete(destPath);
            }
        }
    }

    [Fact]
    public async Task DownloadFileAsync_ShouldReportProgress()
    {
        // Arrange
        var url = "https://www.google.com/robots.txt";
        var destPath = Path.Combine(Path.GetTempPath(), $"test-progress-{Guid.NewGuid()}.txt");
        var progressReports = new List<DownloadProgress>();
        var progress = new Progress<DownloadProgress>(p => progressReports.Add(p));

        try
        {
            // Act
            await DownloadHelper.DownloadFileAsync(url, destPath, progress);

            // Allow progress events to be processed
            await Task.Delay(100);

            // Assert
            progressReports.Should().NotBeEmpty();
            progressReports.First().StatusMessage.Should().Contain("Downloading");
            progressReports.Last().Percentage.Should().Be(1); // 100%
        }
        finally
        {
            if (File.Exists(destPath))
            {
                File.Delete(destPath);
            }
        }
    }

    [Fact]
    public async Task DownloadFileAsync_WithInvalidUrl_ShouldThrow()
    {
        // Arrange
        var url = "https://invalid.nonexistent.domain/file.txt";
        var destPath = Path.Combine(Path.GetTempPath(), $"test-invalid-{Guid.NewGuid()}.txt");

        // Act & Assert
        await Assert.ThrowsAnyAsync<HttpRequestException>(
            () => DownloadHelper.DownloadFileAsync(url, destPath));
    }

    [Fact]
    public async Task DownloadFileAsync_WithCancellation_ShouldThrow()
    {
        // Arrange
        // Use a larger file that won't complete instantly
        var url = "https://www.google.com/images/branding/googlelogo/2x/googlelogo_color_272x92dp.png";
        var destPath = Path.Combine(Path.GetTempPath(), $"test-cancel-{Guid.NewGuid()}.png");
        var cts = new CancellationTokenSource();

        try
        {
            // Cancel immediately
            cts.Cancel();

            // Act & Assert
            await Assert.ThrowsAnyAsync<OperationCanceledException>(
                () => DownloadHelper.DownloadFileAsync(url, destPath, null, cts.Token));
        }
        finally
        {
            if (File.Exists(destPath))
            {
                File.Delete(destPath);
            }
        }
    }
}

public class DownloadProgressTests
{
    [Fact]
    public void DownloadProgress_ShouldHaveAllProperties()
    {
        // Arrange & Act
        var progress = new DownloadProgress
        {
            StatusMessage = "Downloading file.zip...",
            Percentage = 0.5,
            BytesDownloaded = 512 * 1024,
            TotalBytes = 1024 * 1024
        };

        // Assert
        progress.StatusMessage.Should().Be("Downloading file.zip...");
        progress.Percentage.Should().Be(0.5);
        progress.BytesDownloaded.Should().Be(512 * 1024);
        progress.TotalBytes.Should().Be(1024 * 1024);
    }
}
