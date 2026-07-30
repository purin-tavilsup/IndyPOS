using FluentAssertions;
using IndyPOS.Domain.Entities.Core;
using IndyPOS.Domain.Enums;
using IndyPOS.Infrastructure.Persistence.StoreHub.Repositories;
using IndyPOS.Mock;
using Xunit;

namespace IndyPOS.StoreHub.IntegrationTests;

[Collection("Integration")]
public class ProductCategoryRepositoryTests : IntegrationTestBase
{
    public ProductCategoryRepositoryTests(StoreHubWebApplicationFactory factory) : base(factory) { }

    private ProductCategoryRepository CreateRepository(string storeId) =>
        new(GetDbContext(), new MockStoreIdentityService { StoreId = storeId });

    private static ProductCategory Category(string code, int order, ProductCategoryKind kind) => new()
    {
        Code = code,
        DisplayName = code,
        Kind = kind,
        IsEnabled = true,
        DisplayOrder = order,
        CreatedUtc = DateTime.UtcNow,
        LastModifiedUtc = DateTime.UtcNow
    };

    [Fact]
    public async Task GetAllAsync_ShouldReturnOnlyThisStoreOrderedByDisplayOrder()
    {
        var mine = CreateRepository("STORE-REPO-MINE");
        await mine.AddAsync(Category("Toys", 2, ProductCategoryKind.GeneralGoods));
        await mine.AddAsync(Category("Gifts", 1, ProductCategoryKind.GeneralGoods));

        var theirs = CreateRepository("STORE-REPO-THEIRS");
        await theirs.AddAsync(Category("PlumbingMaterials", 1, ProductCategoryKind.Hardware));

        var result = await mine.GetAllAsync();

        result.Select(c => c.Code).Should().Equal("Gifts", "Toys");
    }

    [Fact]
    public async Task AddAsync_ShouldOverwriteACallerSuppliedStoreId()
    {
        // Never trust the caller's store id - the same guard PaymentMethodRepository applies.
        var repository = CreateRepository("STORE-REPO-REAL");
        var category = Category("Toys", 1, ProductCategoryKind.GeneralGoods);
        category.StoreId = "STORE-SOMEONE-ELSE";

        await repository.AddAsync(category);

        (await repository.GetByCodeAsync("Toys")).Should().NotBeNull();
    }

    [Fact]
    public async Task GetByCodeAsync_ForAnotherStoresCode_ShouldReturnNull()
    {
        // Store ids are prefixed so they cannot collide with ProductCategorySeederTests, which
        // seeds "Gifts" under its own ids against this same shared container.
        await CreateRepository("STORE-REPO-A").AddAsync(Category("Gifts", 1, ProductCategoryKind.GeneralGoods));

        (await CreateRepository("STORE-REPO-B").GetByCodeAsync("Gifts")).Should().BeNull();
    }
}
