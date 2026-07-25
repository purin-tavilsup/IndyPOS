using FluentAssertions;
using IndyPOS.Bootstrapper.Installers;

namespace IndyPOS.Bootstrapper.Tests.Installers;

public class ConfigSnapshotTests : IDisposable
{
    private readonly string _dir =
        Path.Combine(Path.GetTempPath(), "indypos-cfgsnap-" + Guid.NewGuid().ToString("N"));

    public ConfigSnapshotTests() => Directory.CreateDirectory(_dir);

    public void Dispose()
    {
        if (Directory.Exists(_dir))
        {
            Directory.Delete(_dir, recursive: true);
        }
    }

    private string WriteFile(string name, string content)
    {
        var path = Path.Combine(_dir, name);
        File.WriteAllText(path, content);
        return path;
    }

    [Fact]
    public void Restore_AfterFileWasOverwritten_ShouldBringBackOriginalContent()
    {
        var path = WriteFile("appsettings.json", """{"ConnectionStrings":{"storehub-db":"DPAPI:abc"}}""");
        var snapshot = ConfigSnapshot.Capture(path);

        File.WriteAllText(path, """{"AllowedHosts":"*"}""");
        snapshot.Restore();

        File.ReadAllText(path).Should().Be("""{"ConnectionStrings":{"storehub-db":"DPAPI:abc"}}""");
    }

    [Fact]
    public void Restore_WhenFileWasAbsentAtCapture_ShouldNotCreateIt()
    {
        // A fresh install has no config yet; restoring must not fabricate one.
        var path = Path.Combine(_dir, "appsettings.json");
        var snapshot = ConfigSnapshot.Capture(path);

        snapshot.Restore();

        File.Exists(path).Should().BeFalse();
    }

    [Fact]
    public void Restore_WhenFileWasAbsentAtCaptureButCreatedSince_ShouldRemoveIt()
    {
        // Extraction dropped a template where there was nothing: leaving it behind
        // would make a failed first install look half-configured.
        var path = Path.Combine(_dir, "appsettings.json");
        var snapshot = ConfigSnapshot.Capture(path);
        File.WriteAllText(path, """{"AllowedHosts":"*"}""");

        snapshot.Restore();

        File.Exists(path).Should().BeFalse();
    }

    [Fact]
    public void Restore_CalledTwice_ShouldBeIdempotent()
    {
        var path = WriteFile("appsettings.json", "original");
        var snapshot = ConfigSnapshot.Capture(path);
        File.WriteAllText(path, "clobbered");

        snapshot.Restore();
        snapshot.Restore();

        File.ReadAllText(path).Should().Be("original");
    }

    [Fact]
    public void Restore_ShouldPreserveExactBytes()
    {
        // The connection string is DPAPI base64; a re-encoding would corrupt it.
        var path = Path.Combine(_dir, "appsettings.json");
        var original = new byte[] { 0xEF, 0xBB, 0xBF, 0x7B, 0x22, 0x61, 0x22, 0x7D };
        File.WriteAllBytes(path, original);
        var snapshot = ConfigSnapshot.Capture(path);

        File.WriteAllText(path, "clobbered");
        snapshot.Restore();

        File.ReadAllBytes(path).Should().Equal(original);
    }
}
