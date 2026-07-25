using FluentAssertions;
using IndyPOS.Application.Abstractions.StoreHub.Repositories;
using IndyPOS.Application.Common.Enums;
using IndyPOS.Application.Common.Exceptions;
using IndyPOS.Application.UseCases.StoreHub.Products;
using IndyPOS.Application.UseCases.StoreHub.Products.Update;
using IndyPOS.Domain.Enums;
using IndyPOS.Mock;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace IndyPOS.Application.Tests.StoreHub.Products;

public class UpdateProductCommandHandlerTests
{
    private static readonly Guid ProductId = Guid.NewGuid();

    private static UpdateProductCommandHandler NewSut(Mock<IProductRepository> products, StoreType storeType) =>
        new(products.Object,
            new MockStoreIdentityService { StoreType = storeType },
            NullLogger<UpdateProductCommandHandler>.Instance);

    private static UpdateProductCommand Command(string category) => new()
    {
        Id = ProductId, Barcode = "8850000000099", Name = "Test", Category = category, UnitPrice = 10m
    };

    private static Domain.Entities.Core.Product ExistingProduct() => new()
    {
        Id = ProductId,
        Barcode = "8850000000099",
        Name = "Test",
        Category = nameof(ProductCategory.GeneralGoods),
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
                .ReturnsAsync((Domain.Entities.Core.Product?)null);
        var sut = NewSut(products, StoreType.GeneralHardware);

        var act = () => sut.HandleAsync(Command(nameof(ProductCategory.GeneralGoods)));

        await act.Should().ThrowAsync<ProductNotFoundException>();
    }

    [Fact]
    public async Task HandleAsync_DuplicateBarcode_ShouldThrowInvalidOperation()
    {
        var products = NewProductsMock();
        products.Setup(r => r.ExistsByBarcodeAsync(It.IsAny<string>(), It.IsAny<Guid?>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);
        var sut = NewSut(products, StoreType.GeneralHardware);

        var act = () => sut.HandleAsync(Command(nameof(ProductCategory.GeneralGoods)));

        // Stays InvalidOperationException (409) — only the not-found case becomes a 404.
        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task HandleAsync_HardwareCategory_OnGeneralOnlyStore_ShouldThrow()
    {
        var products = NewProductsMock();
        var sut = NewSut(products, StoreType.Minimart);

        var act = () => sut.HandleAsync(Command(nameof(ProductCategory.Hardware)));

        await act.Should().ThrowAsync<InvalidOperationException>();
        products.Verify(r => r.UpdateAsync(It.IsAny<Domain.Entities.Core.Product>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task HandleAsync_HardwareCategory_OnGeneralHardwareStore_ShouldSucceed()
    {
        var products = NewProductsMock();
        var sut = NewSut(products, StoreType.GeneralHardware);

        var result = await sut.HandleAsync(Command(nameof(ProductCategory.Hardware)));

        result.Should().NotBeNull();
        products.Verify(r => r.UpdateAsync(It.IsAny<Domain.Entities.Core.Product>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_GeneralGoodsCategory_OnGeneralOnlyStore_ShouldSucceed()
    {
        var products = NewProductsMock();
        var sut = NewSut(products, StoreType.Minimart);

        var result = await sut.HandleAsync(Command(nameof(ProductCategory.GeneralGoods)));

        result.Should().NotBeNull();
        products.Verify(r => r.UpdateAsync(It.IsAny<Domain.Entities.Core.Product>(), It.IsAny<CancellationToken>()), Times.Once);
    }
}
