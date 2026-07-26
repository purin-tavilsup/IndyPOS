using FluentAssertions;
using IndyPOS.Bootstrapper.Installers;

namespace IndyPOS.Bootstrapper.Tests.Installers;

public class StoreHubPayloadTests : IDisposable
{
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
        // The test host has no embedded StoreHub.zip and no sibling folder, so this
        // exercises the "binaries not found" branch without a 67 MB fixture.
        var dest = Path.Combine(_dir, "StoreHub");
        Directory.CreateDirectory(dest);

        var result = await StoreHubPayload.ExtractAsync(dest, log: null, CancellationToken.None);

        result.Should().BeFalse();
    }

    [Fact]
    public async Task ExtractAsync_WithNoPayloadAvailable_ShouldReportEveryLocationItTried()
    {
        var messages = new List<string>();
        var dest = Path.Combine(_dir, "StoreHub");
        Directory.CreateDirectory(dest);

        await StoreHubPayload.ExtractAsync(
            dest, new Progress<string>(messages.Add), CancellationToken.None);

        // Progress<T> marshals asynchronously; drain before asserting.
        await Task.Delay(50);
        messages.Should().Contain(m => m.Contains("StoreHub.zip"));
    }
}
