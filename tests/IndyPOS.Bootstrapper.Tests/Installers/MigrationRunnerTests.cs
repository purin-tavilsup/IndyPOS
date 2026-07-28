using FluentAssertions;
using IndyPOS.Bootstrapper.Installers;

namespace IndyPOS.Bootstrapper.Tests.Installers;

public class MigrationRunnerTests
{
    [Theory]
    [InlineData("ADMIN_SEEDED=true", true)]
    [InlineData("admin_seeded=TRUE", true)]
    [InlineData("noise\nADMIN_SEEDED=true\nmore noise", true)]
    [InlineData("ADMIN_SEEDED=false", false)]
    [InlineData("", false)]
    public void ParseAdminSeeded_WithVariousStdout_ShouldReturnExpected(string stdout, bool expected)
    {
        MigrationRunner.ParseAdminSeeded(stdout).Should().Be(expected);
    }

    [Fact]
    public async Task RunAsync_WhenTheExecutableIsMissing_ShouldFailWithTheProbedPath()
    {
        var missing = Path.Combine(Path.GetTempPath(), "indypos-no-such-" + Guid.NewGuid().ToString("N"));

        var result = await MigrationRunner.RunAsync(missing, log: null, CancellationToken.None);

        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("IndyPOS.StoreHub.exe");
    }
}
