using FluentAssertions;
using IndyPOS.Application.Abstractions.StoreHub.Repositories;
using IndyPOS.Application.Common.Constants;
using IndyPOS.Application.Common.Exceptions;
using IndyPOS.Application.UseCases.StoreHub.Products.Create;
using IndyPOS.Domain.Entities.Core;
using IndyPOS.Domain.Enums;
using IndyPOS.Mock;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace IndyPOS.Application.Tests.UseCases.StoreHub.Products;

public class CreateProductCategoryGateTests
{
    private static ProductCategory Category(string code, ProductCategoryKind kind, bool isEnabled = true) => new()
    {
        StoreId = "STORE-A", Code = code, DisplayName = code, Kind = kind,
        IsEnabled = isEnabled, DisplayOrder = 1,
        CreatedUtc = DateTime.UtcNow, LastModifiedUtc = DateTime.UtcNow
    };

    private static CreateProductCommandHandler BuildHandler(StoreType storeType, ProductCategory? category)
    {
        var products = new Mock<IProductRepository>();
        products.Setup(r => r.ExistsByBarcodeAsync(
                    It.IsAny<string>(), It.IsAny<Guid?>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(false);

        var categories = new Mock<IProductCategoryRepository>();
        categories.Setup(r => r.GetByCodeAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                  .ReturnsAsync(category);

        return new CreateProductCommandHandler(
            products.Object,
            Moq.Mock.Of<IInventoryMovementRepository>(),
            categories.Object,
            new MockStoreIdentityService { StoreType = storeType },
            NullLogger<CreateProductCommandHandler>.Instance);
    }

    private static CreateProductCommand CommandWithCategory(string category) => new()
    {
        Barcode = "8850000000099", Name = "Test", Category = category, UnitPrice = 10m
    };

    [Fact]
    public async Task HandleAsync_WithAnUnknownCategoryCode_ShouldThrowUnknownProductCategory()
    {
        // A code the catalogue does not define is a malformed request (400), not a conflict.
        // Accepting it would put a product in a category nothing can resolve - the failure mode
        // that made 'Other' payments unreportable.
        var handler = BuildHandler(StoreType.GeneralHardware, category: null);

        var act = async () => await handler.HandleAsync(
            CommandWithCategory("NoSuchCategory"), CancellationToken.None);

        await act.Should().ThrowAsync<UnknownProductCategoryException>();
    }

    [Fact]
    public async Task HandleAsync_WithADisabledCategory_ShouldThrowUnknownProductCategory()
    {
        // Spec section 7: a disabled category is treated exactly as an unknown one on a NEW
        // product. The picker hides it too, but the server is the boundary — enforcing this only
        // in the UI would rebuild the client-trusting guard this epic set out to remove.
        var handler = BuildHandler(StoreType.GeneralHardware,
            Category(ProductCategoryCodes.Beverages, ProductCategoryKind.GeneralGoods, isEnabled: false));

        var act = async () => await handler.HandleAsync(
            CommandWithCategory(ProductCategoryCodes.Beverages), CancellationToken.None);

        await act.Should().ThrowAsync<UnknownProductCategoryException>();
    }

    [Fact]
    public async Task HandleAsync_WithALegacyCategoryValue_ShouldThrowRatherThanAccept()
    {
        // Guard for the migration epic. Products written before this epic carried "Hardware" or
        // "GeneralGoods", and the SQLite migration writes raw legacy ids like "50". None of those
        // are catalogue codes. No v4 store has ever run, so no such row exists today — this test
        // exists so the migration cannot start writing them without a red build.
        foreach (var legacyValue in new[] { "Hardware", "GeneralGoods", "50", "10" })
        {
            var handler = BuildHandler(StoreType.GeneralHardware, category: null);

            var act = async () => await handler.HandleAsync(
                CommandWithCategory(legacyValue), CancellationToken.None);

            await act.Should().ThrowAsync<UnknownProductCategoryException>(
                $"'{legacyValue}' is a legacy value, not a catalogue code");
        }
    }

    [Fact]
    public async Task HandleAsync_WithAHardwareCategoryOnAGeneralOnlyStore_ShouldThrowInvalidOperation()
    {
        // Existing product-type restriction behaviour, now driven by Kind rather than by
        // comparing the category string to an enum name. Maps to 409.
        var handler = BuildHandler(StoreType.Minimart,
            Category(ProductCategoryCodes.PlumbingMaterials, ProductCategoryKind.Hardware));

        var act = async () => await handler.HandleAsync(
            CommandWithCategory(ProductCategoryCodes.PlumbingMaterials), CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>()
                 .WithMessage("*not available*");
    }

    [Fact]
    public async Task HandleAsync_WithAHardwareCategoryOnAMultiTypeStore_ShouldSucceed()
    {
        var handler = BuildHandler(StoreType.GeneralHardware,
            Category(ProductCategoryCodes.PlumbingMaterials, ProductCategoryKind.Hardware));

        var act = async () => await handler.HandleAsync(
            CommandWithCategory(ProductCategoryCodes.PlumbingMaterials), CancellationToken.None);

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task HandleAsync_WithAGeneralGoodsCategoryOnAGeneralOnlyStore_ShouldSucceed()
    {
        var handler = BuildHandler(StoreType.Minimart,
            Category(ProductCategoryCodes.Beverages, ProductCategoryKind.GeneralGoods));

        var act = async () => await handler.HandleAsync(
            CommandWithCategory(ProductCategoryCodes.Beverages), CancellationToken.None);

        await act.Should().NotThrowAsync();
    }
}
