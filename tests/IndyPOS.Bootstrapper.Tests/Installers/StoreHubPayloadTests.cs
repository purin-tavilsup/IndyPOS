using FluentAssertions;
using IndyPOS.Bootstrapper.Installers;

namespace IndyPOS.Bootstrapper.Tests.Installers;

public class StoreHubPayloadTests : IDisposable
{
    // A resource name guaranteed not to exist and an empty probe directory (distinct
    // from the destination folder), so these tests exercise the "binaries not found"
    // branch deterministically - regardless of whether this machine has ever run
    // build-installer.ps1, which stages a real Resources/StoreHub.zip that gets embedded.
    private const string MissingResourceName = "IndyPOS.Bootstrapper.Tests.NoSuchResource.zip";

    private readonly string _dir =
        Path.Combine(Path.GetTempPath(), "indypos-payload-" + Guid.NewGuid().ToString("N"));

    public StoreHubPayloadTests() => Directory.CreateDirectory(_dir);

    public void Dispose()
    {
        if (Directory.Exists(_dir))
        {
            Directory.Delete(_dir, recursive: true);
        }
    }

    [Fact]
    public async Task ExtractAsync_WithNoPayloadAvailable_ShouldReturnFalse()
    {
        var dest = Path.Combine(_dir, "Dest");
        Directory.CreateDirectory(dest);

        var result = await StoreHubPayload.ExtractAsync(
            dest, log: null, CancellationToken.None, MissingResourceName, probeDirectory: _dir);

        result.Should().BeFalse();
    }

    [Fact]
    public async Task ExtractAsync_WithNoPayloadAvailable_ShouldReportEveryLocationItTried()
    {
        var messages = new List<string>();
        var dest = Path.Combine(_dir, "Dest");
        Directory.CreateDirectory(dest);

        await StoreHubPayload.ExtractAsync(
            dest, new Progress<string>(messages.Add), CancellationToken.None, MissingResourceName, probeDirectory: _dir);

        // Progress<T> marshals asynchronously; drain before asserting.
        await Task.Delay(50);
        messages.Should().Contain(m => m.Contains("StoreHub.zip"));
    }
}
