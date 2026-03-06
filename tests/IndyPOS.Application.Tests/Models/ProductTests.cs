using FluentAssertions;
using IndyPOS.Application.Common.Models;
using Xunit;

namespace IndyPOS.Application.Tests.Models;

public class ProductTests
{
    [Fact]
    public void GetTotal_WhenNotGroupProduct_ReturnsUnitPriceTimesQuantity()
    {
        // Arrange
        var product = new Product
        {
            UnitPrice = 100m,
            Quantity = 3,
            IsGroupProduct = false,
            GroupPrice = 250m // Should be ignored
        };

        // Act
        var total = product.GetTotal();

        // Assert
        total.Should().Be(300m);
    }

    [Fact]
    public void GetTotal_WhenGroupProduct_ReturnsGroupPrice()
    {
        // Arrange
        var product = new Product
        {
            UnitPrice = 100m,
            Quantity = 3,
            IsGroupProduct = true,
            GroupPrice = 250m
        };

        // Act
        var total = product.GetTotal();

        // Assert
        total.Should().Be(250m);
    }

    [Fact]
    public void GetTotal_WhenQuantityIsZero_ReturnsZero()
    {
        // Arrange
        var product = new Product
        {
            UnitPrice = 100m,
            Quantity = 0,
            IsGroupProduct = false
        };

        // Act
        var total = product.GetTotal();

        // Assert
        total.Should().Be(0m);
    }

    [Fact]
    public void GetTotal_WhenNegativeQuantity_ReturnsNegativeTotal()
    {
        // Arrange (refund scenario)
        var product = new Product
        {
            UnitPrice = 100m,
            Quantity = -2,
            IsGroupProduct = false
        };

        // Act
        var total = product.GetTotal();

        // Assert
        total.Should().Be(-200m);
    }
}
