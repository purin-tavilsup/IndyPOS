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

namespace IndyPOS.Application.Tests.UseCases.StoreHub.Products;

/// <summary>
/// The update handler's category gate must stay byte-identical to the create handler's, so that
/// the two cannot drift. <see cref="CreateProductCategoryGateTests"/> covers the create side;
/// without this mirror, deleting the gate from the update handler would leave a green suite.
/// </summary>
public class UpdateProductCategoryGateTests
{
    private static readonly Guid ProductId = Guid.NewGuid();

    private static ProductCategory Category(string code, ProductCategoryKind kind, bool isEnabled = true) => new()
    {
        StoreId = "STORE-A", Code = code, DisplayName = code, Kind = kind,
        IsEnabled = isEnabled, DisplayOrder = 1,
        CreatedUtc = DateTime.UtcNow, LastModifiedUtc = DateTime.UtcNow
    };

    private static UpdateProductCommandHandler BuildHandler(StoreType storeType, ProductCategory? category)
    {
        var products = new Mock<IProductRepository>();
        products.Setup(r => r.GetByIdAsync(ProductId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new Product
                {
                    Id = ProductId,
                    Barcode = "8850000000099",
                    Name = "Test",
                    Category = ProductCategoryCodes.Beverages,
                    UnitPrice = 10m,
                    IsActive = true,
                    CreatedUtc = DateTime.UtcNow,
                    LastModifiedUtc = DateTime.UtcNow
                });
        products.Setup(r => r.ExistsByBarcodeAsync(
                    It.IsAny<string>(), It.IsAny<Guid?>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(false);

        var categories = new Mock<IProductCategoryRepository>();
        categories.Setup(r => r.GetByCodeAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                  .ReturnsAsync(category);

        return new UpdateProductCommandHandler(
            products.Object,
            categories.Object,
            new MockStoreIdentityService { StoreType = storeType },
            NullLogger<UpdateProductCommandHandler>.Instance);
    }

    private static UpdateProductCommand CommandWithCategory(string category) => new()
    {
        Id = ProductId, Barcode = "8850000000099", Name = "Test", Category = category, UnitPrice = 10m
    };

    [Fact]
    public async Task HandleAsync_WithAnUnknownCategoryCode_ShouldThrowUnknownProductCategory()
    {
        var handler = BuildHandler(StoreType.GeneralHardware, category: null);

        var act = async () => await handler.HandleAsync(
            CommandWithCategory("NoSuchCategory"), CancellationToken.None);

        await act.Should().ThrowAsync<UnknownProductCategoryException>();
    }

    [Fact]
    public async Task HandleAsync_WithADisabledCategory_ShouldThrowUnknownProductCategory()
    {
        var handler = BuildHandler(StoreType.GeneralHardware,
            Category(ProductCategoryCodes.Beverages, ProductCategoryKind.GeneralGoods, isEnabled: false));

        var act = async () => await handler.HandleAsync(
            CommandWithCategory(ProductCategoryCodes.Beverages), CancellationToken.None);

        await act.Should().ThrowAsync<UnknownProductCategoryException>();
    }

    [Fact]
    public async Task HandleAsync_WithAHardwareCategoryOnAGeneralOnlyStore_ShouldThrowInvalidOperation()
    {
        var handler = BuildHandler(StoreType.Minimart,
            Category(ProductCategoryCodes.PlumbingMaterials, ProductCategoryKind.Hardware));

        var act = async () => await handler.HandleAsync(
            CommandWithCategory(ProductCategoryCodes.PlumbingMaterials), CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>()
                 .WithMessage("*not available*");
    }

    [Fact]
    public async Task HandleAsync_WithAServiceCategoryOnAGeneralOnlyStore_ShouldThrowInvalidOperation()
    {
        // MimyShop seeds a Services category but cannot use it until Product.IsTrackable exists,
        // so the gate must fail closed rather than let a service product through.
        var handler = BuildHandler(StoreType.MimyShop,
            Category(ProductCategoryCodes.Services, ProductCategoryKind.Service));

        var act = async () => await handler.HandleAsync(
            CommandWithCategory(ProductCategoryCodes.Services), CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>();
    }
}
