using FluentAssertions;
using IndyPOS.Application.Abstractions.StoreHub.Repositories;
using IndyPOS.Application.Common.Constants;
using IndyPOS.Application.Common.Exceptions;
using IndyPOS.Application.UseCases.StoreHub.Products.Update;
using IndyPOS.Domain.Entities.Core;
using IndyPOS.Domain.Enums;
using IndyPOS.Mock;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace IndyPOS.Application.Tests.StoreHub.Products;

public class UpdateProductCommandHandlerTests
{
    private static readonly Guid ProductId = Guid.NewGuid();

    /// <summary>
    /// Resolves the two codes these tests use from the store's catalogue. The gate reads the
    /// category's Kind now, so the arrangement supplies a catalogue rather than relying on the
    /// category string matching an enum name.
    /// </summary>
    private static IProductCategoryRepository Categories()
    {
        var categories = new Mock<IProductCategoryRepository>();
        categories.Setup(r => r.GetByCodeAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                  .ReturnsAsync((string code, CancellationToken _) => new ProductCategory
                  {
                      StoreId = "test-store",
                      Code = code,
                      DisplayName = code,
                      Kind = code == ProductCategoryCodes.PlumbingMaterials
                          ? ProductCategoryKind.Hardware
                          : ProductCategoryKind.GeneralGoods,
                      IsEnabled = true,
                      DisplayOrder = 1,
                      CreatedUtc = DateTime.UtcNow,
                      LastModifiedUtc = DateTime.UtcNow
                  });
        return categories.Object;
    }

    private static UpdateProductCommandHandler NewSut(Mock<IProductRepository> products, StoreType storeType) =>
        new(products.Object,
            Categories(),
            new MockStoreIdentityService { StoreType = storeType },
            NullLogger<UpdateProductCommandHandler>.Instance);

    private static UpdateProductCommand Command(string category) => new()
    {
        Id = ProductId, Barcode = "8850000000099", Name = "Test", Category = category, UnitPrice = 10m
    };

    private static Product ExistingProduct() => new()
    {
        Id = ProductId,
        Barcode = "8850000000099",
        Name = "Test",
        Category = ProductCategoryCodes.Beverages,
        UnitPrice = 10m,
        IsActive = true,
        CreatedUtc = DateTime.UtcNow,
        LastModifiedUtc = DateTime.UtcNow
    };

    private static Mock<IProductRepository> NewProductsMock()
    {
        var products = new Mock<IProductRepository>();
        products.Setup(r => r.GetByIdAsync(ProductId, It.IsAny<CancellationToken>())).ReturnsAsync(ExistingProduct());
        products.Setup(r => r.ExistsByBarcodeAsync(It.IsAny<string>(), It.IsAny<Guid?>(), It.IsAny<CancellationToken>())).ReturnsAsync(false);
        return products;
    }

    [Fact]
    public async Task HandleAsync_UnknownProductId_ShouldThrowProductNotFound()
    {
        var products = NewProductsMock();
        products.Setup(r => r.GetByIdAsync(ProductId, It.IsAny<CancellationToken>()))
                .ReturnsAsync((Product?)null);
        var sut = NewSut(products, StoreType.GeneralHardware);

        var act = () => sut.HandleAsync(Command(ProductCategoryCodes.Beverages));

        await act.Should().ThrowAsync<ProductNotFoundException>();
        products.Verify(r => r.UpdateAsync(It.IsAny<Product>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task HandleAsync_DuplicateBarcode_ShouldThrowInvalidOperation()
    {
        var products = NewProductsMock();
        products.Setup(r => r.ExistsByBarcodeAsync(It.IsAny<string>(), It.IsAny<Guid?>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);
        var sut = NewSut(products, StoreType.GeneralHardware);

        var act = () => sut.HandleAsync(Command(ProductCategoryCodes.Beverages));

        // Stays InvalidOperationException (409) — only the not-found case becomes a 404. The
        // NotBeOfType guard keeps this honest if ProductNotFoundException is ever re-parented
        // under InvalidOperationException, which would silently restore the 409.
        var thrown = await act.Should().ThrowAsync<InvalidOperationException>();
        thrown.Which.Should().NotBeOfType<ProductNotFoundException>();
    }

    [Fact]
    public async Task HandleAsync_HardwareCategory_OnGeneralOnlyStore_ShouldThrow()
    {
        var products = NewProductsMock();
        var sut = NewSut(products, StoreType.Minimart);

        var act = () => sut.HandleAsync(Command(ProductCategoryCodes.PlumbingMaterials));

        await act.Should().ThrowAsync<InvalidOperationException>();
        products.Verify(r => r.UpdateAsync(It.IsAny<Product>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task HandleAsync_HardwareCategory_OnGeneralHardwareStore_ShouldSucceed()
    {
        var products = NewProductsMock();
        var sut = NewSut(products, StoreType.GeneralHardware);

        var result = await sut.HandleAsync(Command(ProductCategoryCodes.PlumbingMaterials));

        result.Should().NotBeNull();
        products.Verify(r => r.UpdateAsync(It.IsAny<Product>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_GeneralGoodsCategory_OnGeneralOnlyStore_ShouldSucceed()
    {
        var products = NewProductsMock();
        var sut = NewSut(products, StoreType.Minimart);

        var result = await sut.HandleAsync(Command(ProductCategoryCodes.Beverages));

        result.Should().NotBeNull();
        products.Verify(r => r.UpdateAsync(It.IsAny<Product>(), It.IsAny<CancellationToken>()), Times.Once);
    }
}
