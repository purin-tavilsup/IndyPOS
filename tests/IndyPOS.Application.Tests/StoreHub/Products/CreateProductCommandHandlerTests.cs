using FluentAssertions;
using IndyPOS.Application.Abstractions.StoreHub.Repositories;
using IndyPOS.Application.Common.Constants;
using IndyPOS.Application.UseCases.StoreHub.Products.Create;
using IndyPOS.Domain.Entities.Core;
using IndyPOS.Domain.Enums;
using IndyPOS.Mock;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace IndyPOS.Application.Tests.StoreHub.Products;

public class CreateProductCommandHandlerTests
{
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

    private static CreateProductCommandHandler NewSut(Mock<IProductRepository> products, StoreType storeType) =>
        new(products.Object,
            Moq.Mock.Of<IInventoryMovementRepository>(),
            Categories(),
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

        var act = () => sut.HandleAsync(Command(ProductCategoryCodes.PlumbingMaterials));

        await act.Should().ThrowAsync<InvalidOperationException>();
        products.Verify(r => r.AddAsync(It.IsAny<Product>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task HandleAsync_HardwareCategory_OnGeneralHardwareStore_ShouldSucceed()
    {
        var products = new Mock<IProductRepository>();
        products.Setup(r => r.ExistsByBarcodeAsync(It.IsAny<string>(), It.IsAny<Guid?>(), It.IsAny<CancellationToken>())).ReturnsAsync(false);
        var sut = NewSut(products, StoreType.GeneralHardware);

        var result = await sut.HandleAsync(Command(ProductCategoryCodes.PlumbingMaterials));

        result.Should().NotBeNull();
        products.Verify(r => r.AddAsync(It.IsAny<Product>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_GeneralGoodsCategory_OnGeneralOnlyStore_ShouldSucceed()
    {
        var products = new Mock<IProductRepository>();
        products.Setup(r => r.ExistsByBarcodeAsync(It.IsAny<string>(), It.IsAny<Guid?>(), It.IsAny<CancellationToken>())).ReturnsAsync(false);
        var sut = NewSut(products, StoreType.Minimart);

        var result = await sut.HandleAsync(Command(ProductCategoryCodes.Beverages));

        result.Should().NotBeNull();
        products.Verify(r => r.AddAsync(It.IsAny<Product>(), It.IsAny<CancellationToken>()), Times.Once);
    }
}
