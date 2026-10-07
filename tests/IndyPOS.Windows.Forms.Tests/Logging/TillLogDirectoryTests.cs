using FluentAssertions;
using IndyPOS.Application.Common;
using Xunit;

namespace IndyPOS.Windows.Forms.Tests.Logging;

// Several dev tills (--store all), and the installed till, would otherwise write one log file.
public class TillLogDirectoryTests
{
    private const string DevDirectory = @"C:\dev\till-logs";

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Resolve_WithoutAnOverride_UsesTheInstalledLogsDirectory(string? overrideDirectory)
    {
        TillLogDirectory.Resolve(overrideDirectory).Should()
                                                   .Be(InstallPaths.LogsDirectory);
    }

    [Fact]
    public void Resolve_WithAnOverride_UsesIt()
    {
        TillLogDirectory.Resolve(DevDirectory).Should()
                                              .Be(DevDirectory);
    }
}
