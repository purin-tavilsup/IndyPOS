using FluentAssertions;
using IndyPOS.Application.Abstractions.StoreHub.Repositories;
using IndyPOS.Application.Common.Enums;
using IndyPOS.Application.UseCases.StoreHub.Products;
using IndyPOS.Application.UseCases.StoreHub.Products.Create;
using IndyPOS.Domain.Enums;
using IndyPOS.Mock;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace IndyPOS.Application.Tests.StoreHub.Products;

public class CreateProductCommandHandlerTests
{
    private static CreateProductCommandHandler NewSut(Mock<IProductRepository> products, StoreType storeType) =>
        new(products.Object,
            Moq.Mock.Of<IInventoryMovementRepository>(),
            new MockStoreIdentityService { StoreType = storeType },
            NullLogger<CreateProductCommandHandler>.Instance);

    private static CreateProductCommand Command(string category) => new()
    {
        Barcode = "8850000000099", Name = "Test", Category = category, UnitPrice = 10m
    };

    [Fact]
    public async Task HandleAsync_HardwareCategory_OnGeneralOnlyStore_ShouldThrow()
    {
        var products = new Mock<IProductRepository>();
        products.Setup(r => r.ExistsByBarcodeAsync(It.IsAny<string>(), It.IsAny<Guid?>(), It.IsAny<CancellationToken>())).ReturnsAsync(false);
        var sut = NewSut(products, StoreType.Minimart);

        var act = () => sut.HandleAsync(Command(nameof(ProductCategory.Hardware)));

        await act.Should().ThrowAsync<InvalidOperationException>();
        products.Verify(r => r.AddAsync(It.IsAny<Domain.Entities.Core.Product>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task HandleAsync_HardwareCategory_OnGeneralHardwareStore_ShouldSucceed()
    {
        var products = new Mock<IProductRepository>();
        products.Setup(r => r.ExistsByBarcodeAsync(It.IsAny<string>(), It.IsAny<Guid?>(), It.IsAny<CancellationToken>())).ReturnsAsync(false);
        var sut = NewSut(products, StoreType.GeneralHardware);

        var result = await sut.HandleAsync(Command(nameof(ProductCategory.Hardware)));

        result.Should().NotBeNull();
        products.Verify(r => r.AddAsync(It.IsAny<Domain.Entities.Core.Product>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_GeneralGoodsCategory_OnGeneralOnlyStore_ShouldSucceed()
    {
        var products = new Mock<IProductRepository>();
        products.Setup(r => r.ExistsByBarcodeAsync(It.IsAny<string>(), It.IsAny<Guid?>(), It.IsAny<CancellationToken>())).ReturnsAsync(false);
        var sut = NewSut(products, StoreType.Minimart);

        var result = await sut.HandleAsync(Command(nameof(ProductCategory.GeneralGoods)));

        result.Should().NotBeNull();
        products.Verify(r => r.AddAsync(It.IsAny<Domain.Entities.Core.Product>(), It.IsAny<CancellationToken>()), Times.Once);
    }
}
