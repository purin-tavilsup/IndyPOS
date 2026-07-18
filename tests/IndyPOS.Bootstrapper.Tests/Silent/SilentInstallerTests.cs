using FluentAssertions;
using IndyPOS.Bootstrapper.Silent;

namespace IndyPOS.Bootstrapper.Tests.Silent;

public class SilentInstallerTests
{
    [Fact]
    public void Run_WithUsageErrorParseResult_ShouldReturnExitOne()
    {
        var parse = ParseResult.Usage("--silent requires --store-id <ID>.");

        var exit = SilentInstaller.Run(parse);

        exit.Should().Be(1);
    }
}
