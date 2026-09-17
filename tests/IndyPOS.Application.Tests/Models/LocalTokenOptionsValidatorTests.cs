namespace IndyPOS.Application.Tests.Models;

using System;
using FluentAssertions;
using IndyPOS.Application.Common.Models;
using Xunit;

public class LocalTokenOptionsValidatorTests
{
    private static LocalTokenOptions ConfiguredOptions() => new()
    {
        SecretKey = "eEq0T1u+PzvR7mQ3nS9xW2yB5cF8hJ1kL4oN6pA0dG=="
    };

    [Fact]
    public void EnsureProductionSafe_WithDefaultKeyOutsideDevelopment_ShouldThrow()
    {
        var act = () => LocalTokenOptionsValidator.EnsureProductionSafe(
            new LocalTokenOptions(),
            isDevelopment: false);

        act.Should()
           .Throw<InvalidOperationException>()
           .WithMessage("*LocalToken__SecretKey*");
    }

    [Fact]
    public void EnsureProductionSafe_WithDefaultKeyInDevelopment_ShouldNotThrow()
    {
        var act = () => LocalTokenOptionsValidator.EnsureProductionSafe(
            new LocalTokenOptions(),
            isDevelopment: true);

        act.Should()
           .NotThrow("the Aspire dev loop must keep starting without configuration");
    }

    [Fact]
    public void EnsureProductionSafe_WithConfiguredKeyOutsideDevelopment_ShouldNotThrow()
    {
        var act = () => LocalTokenOptionsValidator.EnsureProductionSafe(
            ConfiguredOptions(),
            isDevelopment: false);

        act.Should()
           .NotThrow();
    }

    [Fact]
    public void EnsureProductionSafe_WithBlankKeyOutsideDevelopment_ShouldThrow()
    {
        var act = () => LocalTokenOptionsValidator.EnsureProductionSafe(
            new LocalTokenOptions { SecretKey = "  " },
            isDevelopment: false);

        act.Should()
           .Throw<InvalidOperationException>();
    }

    [Fact]
    public void EnsureProductionSafe_WithNullOptions_ShouldThrowArgumentNullException()
    {
        var act = () => LocalTokenOptionsValidator.EnsureProductionSafe(null!, isDevelopment: false);

        act.Should()
           .Throw<ArgumentNullException>();
    }
}
