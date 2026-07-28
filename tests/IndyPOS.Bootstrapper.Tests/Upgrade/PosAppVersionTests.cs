using FluentAssertions;
using IndyPOS.Bootstrapper.Upgrade;

namespace IndyPOS.Bootstrapper.Tests.Upgrade;

public class PosAppVersionTests : IDisposable
{
    private readonly string _dir =
        Path.Combine(Path.GetTempPath(), "indypos-sqver-" + Guid.NewGuid().ToString("N"));

    public PosAppVersionTests() => Directory.CreateDirectory(_dir);

    public void Dispose()
    {
        if (Directory.Exists(_dir))
        {
            Directory.Delete(_dir, recursive: true);
        }
    }

    // Verbatim shape captured from the VM on 2026-07-26.
    private const string RealSqVersion = """
        <?xml version="1.0" encoding="utf-8"?>
        <package xmlns="http://schemas.microsoft.com/packaging/2010/07/nuspec.xsd">
        <metadata>
        <id>IndyPOS.POS.v4</id>
        <title>IndyPOS.POS.v4</title>
        <version>4.0.1</version>
        <channel>stable</channel>
        <mainExe>IndyPOS.Windows.Forms.exe</mainExe>
        </metadata>
        </package>
        """;

    [Fact]
    public void Read_WithARealSqVersionFile_ShouldReturnThePackageVersion()
    {
        File.WriteAllText(Path.Combine(_dir, "sq.version"), RealSqVersion);

        PosAppVersion.Read(_dir).Should().Be("4.0.1");
    }

    [Fact]
    public void Read_WhenTheFileIsAbsent_ShouldReturnNull()
    {
        PosAppVersion.Read(_dir).Should().BeNull();
    }

    [Fact]
    public void Read_WhenTheDirectoryIsAbsent_ShouldReturnNull()
    {
        PosAppVersion.Read(Path.Combine(_dir, "nope")).Should().BeNull();
    }

    [Fact]
    public void Read_WithMalformedXml_ShouldReturnNull()
    {
        // Never throw on the reporting path: POS_UPDATED is telemetry, not a gate.
        File.WriteAllText(Path.Combine(_dir, "sq.version"), "<package><metadata>");

        PosAppVersion.Read(_dir).Should().BeNull();
    }

    [Fact]
    public void Read_WithNoVersionElement_ShouldReturnNull()
    {
        File.WriteAllText(Path.Combine(_dir, "sq.version"),
            """<package xmlns="http://schemas.microsoft.com/packaging/2010/07/nuspec.xsd"><metadata><id>x</id></metadata></package>""");

        PosAppVersion.Read(_dir).Should().BeNull();
    }
}
