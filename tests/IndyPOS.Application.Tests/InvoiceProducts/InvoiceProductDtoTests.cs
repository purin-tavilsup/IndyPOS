using FluentAssertions;
using IndyPOS.Application.UseCases.InvoiceProducts;
using Xunit;

namespace IndyPOS.Application.Tests.InvoiceProducts;

public class InvoiceProductDtoTests
{
    [Fact]
    public void GetTotal_WhenNotGroupProduct_ReturnsUnitPriceTimesQuantity()
    {
        // Arrange
        var dto = new InvoiceProductDto(
            InvoiceProductId: 1,
            Priority: 1,
            InvoiceId: 100,
            InventoryProductId: 50,
            Barcode: "123456",
            Description: "Test Product",
            Manufacturer: "Test",
            Brand: "Test",
            Category: 1,
            UnitPrice: 100m,
            Quantity: 3,
            DateCreated: "2024-01-01",
            Note: "",
            GroupPrice: 250m,
            IsGroupProduct: false
        );

        // Act
        var total = dto.GetTotal();

        // Assert
        total.Should().Be(300m);
    }

    [Fact]
    public void GetTotal_WhenGroupProduct_ReturnsGroupPrice()
    {
        // Arrange
        var dto = new InvoiceProductDto(
            InvoiceProductId: 1,
            Priority: 1,
            InvoiceId: 100,
            InventoryProductId: 50,
            Barcode: "123456",
            Description: "Test Product",
            Manufacturer: "Test",
            Brand: "Test",
            Category: 1,
            UnitPrice: 100m,
            Quantity: 3,
            DateCreated: "2024-01-01",
            Note: "Group Price",
            GroupPrice: 250m,
            IsGroupProduct: true
        );

        // Act
        var total = dto.GetTotal();

        // Assert
        total.Should().Be(250m);
    }

    [Fact]
    public void GetTotal_WhenQuantityIsOne_ReturnsUnitPrice()
    {
        // Arrange
        var dto = new InvoiceProductDto(
            InvoiceProductId: 1,
            Priority: 1,
            InvoiceId: 100,
            InventoryProductId: 50,
            Barcode: "123456",
            Description: "Test Product",
            Manufacturer: "Test",
            Brand: "Test",
            Category: 1,
            UnitPrice: 150m,
            Quantity: 1,
            DateCreated: "2024-01-01",
            Note: "",
            GroupPrice: 0m,
            IsGroupProduct: false
        );

        // Act
        var total = dto.GetTotal();

        // Assert
        total.Should().Be(150m);
    }
}
