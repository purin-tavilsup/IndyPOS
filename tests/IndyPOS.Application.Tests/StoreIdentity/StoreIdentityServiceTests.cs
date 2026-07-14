using FluentAssertions;
using IndyPOS.Application.Common.Models;
using IndyPOS.Infrastructure.Services;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace IndyPOS.Application.Tests.StoreIdentity;

public class StoreIdentityServiceTests
{
    [Fact]
    public void StoreId_WhenConfigured_ReturnsConfiguredValue()
    {
        // Arrange
        var options = CreateOptions(id: "STORE-001", name: "Test Store");
        var service = new StoreIdentityService(options);

        // Act
        var result = service.StoreId;

        // Assert
        result.Should().Be("STORE-001");
    }

    [Fact]
    public void StoreId_WhenNotConfigured_ReturnsDefaultBasedOnMachineName()
    {
        // Arrange
        var options = CreateOptions(id: null, name: null);
        var service = new StoreIdentityService(options);

        // Act
        var result = service.StoreId;

        // Assert
        result.Should().StartWith("STORE-");
        result.Should().Contain(Environment.MachineName.ToUpperInvariant());
    }

    [Fact]
    public void StoreName_WhenConfigured_ReturnsConfiguredValue()
    {
        // Arrange
        var options = CreateOptions(id: "STORE-001", name: "Bangkok Branch 1");
        var service = new StoreIdentityService(options);

        // Act
        var result = service.StoreName;

        // Assert
        result.Should().Be("Bangkok Branch 1");
    }

    [Fact]
    public void StoreName_WhenNotConfigured_ReturnsDefaultStore()
    {
        // Arrange
        var options = CreateOptions(id: null, name: null);
        var service = new StoreIdentityService(options);

        // Act
        var result = service.StoreName;

        // Assert
        result.Should().Be("Default Store");
    }

    [Fact]
    public void EnsureConfigured_WhenStoreIdConfigured_DoesNotThrow()
    {
        // Arrange
        var options = CreateOptions(id: "STORE-001", name: "Test Store");
        var service = new StoreIdentityService(options);

        // Act & Assert
        var act = () => service.EnsureConfigured();
        act.Should().NotThrow();
    }

    [Fact]
    public void EnsureConfigured_WhenStoreIdNotConfigured_ThrowsInvalidOperationException()
    {
        // Arrange
        var options = CreateOptions(id: null, name: null);
        var service = new StoreIdentityService(options);

        // Act & Assert
        var act = () => service.EnsureConfigured();
        act.Should().Throw<InvalidOperationException>()
           .WithMessage("*Store.Id is not configured*");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void EnsureConfigured_WhenStoreIdEmptyOrWhitespace_ThrowsInvalidOperationException(string emptyId)
    {
        // Arrange
        var options = CreateOptions(id: emptyId, name: null);
        var service = new StoreIdentityService(options);

        // Act & Assert
        var act = () => service.EnsureConfigured();
        act.Should().Throw<InvalidOperationException>()
           .WithMessage("*Store.Id is not configured*");
    }

    private static IOptions<StoreIdentityOptions> CreateOptions(string? id, string? name)
    {
        var optionsValue = new StoreIdentityOptions
        {
            Id = id,
            Name = name
        };

        var options = new Mock<IOptions<StoreIdentityOptions>>();
        options.Setup(x => x.Value).Returns(optionsValue);

        return options.Object;
    }
}
