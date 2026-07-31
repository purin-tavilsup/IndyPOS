using FluentAssertions;
using IndyPOS.Domain.Entities.Core;
using IndyPOS.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace IndyPOS.StoreHub.IntegrationTests;

[Collection("Integration")]
public class ProductCategoryPersistenceTests : IntegrationTestBase
{
    public ProductCategoryPersistenceTests(StoreHubWebApplicationFactory factory) : base(factory) { }

    [Fact]
    public async Task ProductCategory_ShouldRoundTripThroughPostgres()
    {
        var dbContext = GetDbContext();
        var now = DateTime.UtcNow;

        dbContext.ProductCategories.Add(new ProductCategory
        {
            StoreId = "STORE-A",
            Code = "PlumbingMaterials",
            DisplayName = "วัสดุและอุปกรณ์ระบบประปา",
            Kind = ProductCategoryKind.Hardware,
            IsEnabled = true,
            DisplayOrder = 14,
            CreatedUtc = now,
            LastModifiedUtc = now
        });
        await dbContext.SaveChangesAsync();
        dbContext.ChangeTracker.Clear();

        var loaded = await dbContext.ProductCategories.AsNoTracking()
            .SingleAsync(c => c.StoreId == "STORE-A" && c.Code == "PlumbingMaterials");

        loaded.Kind.Should().Be(ProductCategoryKind.Hardware);
        loaded.DisplayName.Should().Be("วัสดุและอุปกรณ์ระบบประปา", "Thai labels must survive the round trip");
        loaded.DisplayOrder.Should().Be(14);
    }

    [Fact]
    public async Task ProductCategory_ShouldAllowTheSameCodeInDifferentStores()
    {
        // The whole point of store scoping: MimyShop's Toys and GeneralHardware's Toys are
        // different rows, and a global unique constraint would reject the second store.
        var dbContext = GetDbContext();
        var now = DateTime.UtcNow;

        foreach (var storeId in new[] { "STORE-B", "STORE-C" })
        {
            dbContext.ProductCategories.Add(new ProductCategory
            {
                StoreId = storeId, Code = "Toys", DisplayName = "ของเล่น",
                Kind = ProductCategoryKind.GeneralGoods, IsEnabled = true, DisplayOrder = 1,
                CreatedUtc = now, LastModifiedUtc = now
            });
        }

        var act = async () => await dbContext.SaveChangesAsync();

        await act.Should().NotThrowAsync();
    }
}
