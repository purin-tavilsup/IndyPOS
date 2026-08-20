namespace IndyPOS.Application.Tests.Models;

using FluentAssertions;
using IndyPOS.Application.Common.Models;
using Xunit;

public class LocalTokenOptionsTests
{
    [Fact]
    public void UsesBuiltInDefaultSecretKey_WithUntouchedOptions_ShouldReturnTrue()
    {
        var options = new LocalTokenOptions();

        options.UsesBuiltInDefaultSecretKey
               .Should()
               .BeTrue("a freshly constructed instance still carries the repo's published key");
    }

    [Fact]
    public void UsesBuiltInDefaultSecretKey_WithConfiguredKey_ShouldReturnFalse()
    {
        var options = new LocalTokenOptions
        {
            SecretKey = "eEq0T1u+PzvR7mQ3nS9xW2yB5cF8hJ1kL4oN6pA0dG=="
        };

        options.UsesBuiltInDefaultSecretKey
               .Should()
               .BeFalse();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void UsesBuiltInDefaultSecretKey_WithMissingKey_ShouldReturnTrue(string? secretKey)
    {
        var options = new LocalTokenOptions { SecretKey = secretKey! };

        options.UsesBuiltInDefaultSecretKey
               .Should()
               .BeTrue("an unset key is no safer than the default one");
    }

    [Fact]
    public void BuiltInDefaultSecretKey_ShouldBeTheValueThePropertyInitialiserUses()
    {
        new LocalTokenOptions().SecretKey
                               .Should()
                               .Be(LocalTokenOptions.BuiltInDefaultSecretKey);
    }
}
