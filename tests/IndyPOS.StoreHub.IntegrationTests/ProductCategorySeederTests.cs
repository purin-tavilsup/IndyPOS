using FluentAssertions;
using IndyPOS.Application.Common.Constants;
using IndyPOS.Domain.Enums;
using IndyPOS.Infrastructure.Persistence.StoreHub.Repositories;
using IndyPOS.Infrastructure.Persistence.StoreHub.Seeders;
using IndyPOS.Mock;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace IndyPOS.StoreHub.IntegrationTests;

[Collection("Integration")]
public class ProductCategorySeederTests : IntegrationTestBase
{
    public ProductCategorySeederTests(StoreHubWebApplicationFactory factory) : base(factory) { }

    private async Task<IReadOnlyList<string>> SeedAndReadCodesAsync(string storeId, StoreType storeType)
    {
        var identity = new MockStoreIdentityService { StoreId = storeId, StoreType = storeType };
        var repository = new ProductCategoryRepository(GetDbContext(), identity);

        await new ProductCategorySeeder(repository, identity,
            NullLogger<ProductCategorySeeder>.Instance).SeedAsync();

        return (await repository.GetAllAsync()).Select(c => c.Code).ToList();
    }

    [Fact]
    public async Task SeedAsync_ForGeneralHardware_ShouldSeedSixteenIncludingFiveHardware()
    {
        var identity = new MockStoreIdentityService
        {
            StoreId = "STORE-GH", StoreType = StoreType.GeneralHardware
        };
        var repository = new ProductCategoryRepository(GetDbContext(), identity);

        await new ProductCategorySeeder(repository, identity,
            NullLogger<ProductCategorySeeder>.Instance).SeedAsync();

        var seeded = await repository.GetAllAsync();

        seeded.Should().HaveCount(16);
        seeded.Count(c => c.Kind == ProductCategoryKind.Hardware).Should().Be(5);
        seeded.Should().Contain(c => c.Code == ProductCategoryCodes.PlumbingMaterials);
    }

    [Fact]
    public async Task SeedAsync_ForMinimart_ShouldSeedTenAndNoHardware()
    {
        var codes = await SeedAndReadCodesAsync("STORE-MM", StoreType.Minimart);

        codes.Should().HaveCount(10);
        codes.Should().NotContain(ProductCategoryCodes.PlumbingMaterials);
    }

    [Fact]
    public async Task SeedAsync_ForMinimart_ShouldNotSeedTheLeftoverAgricultureCategory()
    {
        // MimyMart's category table was copied from GeneralHardware and การเกษตร came along as a
        // leftover: 0 products and 0 invoice lines in the real database. Seeding it would put a
        // category a minimart never sells in its picker.
        var codes = await SeedAndReadCodesAsync("STORE-MM2", StoreType.Minimart);

        codes.Should().NotContain(ProductCategoryCodes.Agriculture);
    }

    [Fact]
    public async Task SeedAsync_ForMimyShop_ShouldSeedSeventeenIncludingServices()
    {
        // All 17 are seeded even though 12 have no products yet: MimyShop is a new store still
        // adding inventory, so its unused categories are a plan, not detritus.
        var codes = await SeedAndReadCodesAsync("STORE-MS", StoreType.MimyShop);

        codes.Should().HaveCount(17);
        codes.Should().Contain(ProductCategoryCodes.Services);
        codes.Should().Contain(ProductCategoryCodes.Gifts);
    }

    [Fact]
    public async Task SeedAsync_ForMimyShop_ShouldNotSeedMinimartOnlyGroceryCategories()
    {
        // The mix-up this test exists to catch: MimyShop and MimyMart share id ranges with
        // completely different meanings, so a copy-paste between their seed tables is easy.
        var codes = await SeedAndReadCodesAsync("STORE-MS2", StoreType.MimyShop);

        codes.Should().NotContain(ProductCategoryCodes.Beverages);
        codes.Should().NotContain(ProductCategoryCodes.AlcoholicBeverages);
        codes.Should().NotContain(ProductCategoryCodes.Medicine);
    }

    [Fact]
    public async Task SeedAsync_RunTwice_ShouldNotDuplicate()
    {
        var identity = new MockStoreIdentityService
        {
            StoreId = "STORE-TWICE", StoreType = StoreType.MimyShop
        };
        var repository = new ProductCategoryRepository(GetDbContext(), identity);
        var seeder = new ProductCategorySeeder(repository, identity,
            NullLogger<ProductCategorySeeder>.Instance);

        await seeder.SeedAsync();
        await seeder.SeedAsync();

        (await repository.GetAllAsync()).Should().HaveCount(17);
    }

    [Fact]
    public async Task SeedAsync_ShouldGiveEachStoreItsOwnRows()
    {
        await SeedAndReadCodesAsync("STORE-A", StoreType.MimyShop);
        var second = await SeedAndReadCodesAsync("STORE-B", StoreType.Minimart);

        second.Should().HaveCount(10, "STORE-B must not see STORE-A's rows");
    }
}
