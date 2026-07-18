using FluentAssertions;
using IndyPOS.Bootstrapper.Silent;
using Xunit;

namespace IndyPOS.Bootstrapper.Tests.Silent;

public class SecretScrubberTests
{
    [Theory]
    [InlineData("Host=127.0.0.1;Password=SuperSecret;Db=x")]
    [InlineData("Host=127.0.0.1;Pwd=SuperSecret")]
    public void Scrub_WithPasswordAssignment_ShouldRedactValue(string input)
    {
        var result = SecretScrubber.Scrub(input);

        result.Should().NotContain("SuperSecret");
        result.Should().Contain("***REDACTED***");
    }

    [Fact]
    public void Scrub_WithPlainText_ShouldReturnUnchanged()
    {
        SecretScrubber.Scrub("provisioning failed (exit 1)").Should().Be("provisioning failed (exit 1)");
    }

    [Fact]
    public void Scrub_WithNull_ShouldReturnEmpty()
    {
        SecretScrubber.Scrub(null).Should().BeEmpty();
    }
}
