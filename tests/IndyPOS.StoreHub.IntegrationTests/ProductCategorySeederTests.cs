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

    /// <summary>
    /// Pins the exact seeded rows for a store type. The spec's section 9 says the mitigation for
    /// untranslatable Thai labels is that "the integration tests pin whatever is agreed, so an
    /// error is consistent and visible in the seed table rather than silent" — this is that gate.
    /// <para>Counts and Kinds alone cannot catch it: swapping two labels within a store keeps
    /// every count identical, and a label transposed onto the wrong code is exactly the silent
    /// data defect the migration epic then inherits.</para>
    /// </summary>
    private async Task AssertSeededRowsAsync(
        string storeId, StoreType storeType, params (string Code, string DisplayName, ProductCategoryKind Kind)[] expected)
    {
        var identity = new MockStoreIdentityService { StoreId = storeId, StoreType = storeType };
        var repository = new ProductCategoryRepository(GetDbContext(), identity);

        await new ProductCategorySeeder(repository, identity,
            NullLogger<ProductCategorySeeder>.Instance).SeedAsync();

        var seeded = await repository.GetAllAsync();

        seeded.Select(c => (c.Code, c.DisplayName, c.Kind)).Should().Equal(expected);
        seeded.Select(c => c.DisplayOrder).Should().OnlyHaveUniqueItems();
        seeded.Select(c => c.DisplayName).Should().OnlyHaveUniqueItems(
            "the POS resolves a picked DisplayName back to a Code, so duplicates would be ambiguous");
    }

    [Fact]
    public async Task SeedAsync_ForGeneralHardware_ShouldSeedTheExactLegacyLabels()
    {
        // Verified against .planning/indypos-overhaul/sqlite_database/GeneralHardware/Store.db,
        // legacy ProductCategory ids 10-20 then 50-54, in that order.
        await AssertSeededRowsAsync("STORE-LABELS-GH", StoreType.GeneralHardware,
            (ProductCategoryCodes.Miscellaneous, "เบ็ดเตล็ด", ProductCategoryKind.GeneralGoods),
            (ProductCategoryCodes.Beverages, "เครื่องดื่ม", ProductCategoryKind.GeneralGoods),
            (ProductCategoryCodes.Snacks, "ขนม", ProductCategoryKind.GeneralGoods),
            (ProductCategoryCodes.AlcoholicBeverages, "เครื่องดื่มแอลกอฮอล์", ProductCategoryKind.GeneralGoods),
            (ProductCategoryCodes.Food, "อาหาร", ProductCategoryKind.GeneralGoods),
            (ProductCategoryCodes.Stationery, "เครื่องเขียน", ProductCategoryKind.GeneralGoods),
            (ProductCategoryCodes.Household, "ของใช้ในบ้าน", ProductCategoryKind.GeneralGoods),
            (ProductCategoryCodes.ElectricalAppliances, "เครื่องใช้ไฟฟ้า", ProductCategoryKind.GeneralGoods),
            (ProductCategoryCodes.Toys, "ของเล่น", ProductCategoryKind.GeneralGoods),
            (ProductCategoryCodes.Medicine, "ยา", ProductCategoryKind.GeneralGoods),
            (ProductCategoryCodes.Agriculture, "การเกษตร", ProductCategoryKind.GeneralGoods),
            (ProductCategoryCodes.GeneralMaterials, "วัสดุและอุปกรณ์ทั่วไป", ProductCategoryKind.Hardware),
            (ProductCategoryCodes.MaterialsAndEquipment, "วัสดุและอุปกรณ์", ProductCategoryKind.Hardware),
            (ProductCategoryCodes.PlumbingMaterials, "วัสดุและอุปกรณ์ระบบประปา", ProductCategoryKind.Hardware),
            (ProductCategoryCodes.ElectricalMaterials, "วัสดุและอุปกรณ์ระบบไฟฟ้า", ProductCategoryKind.Hardware),
            (ProductCategoryCodes.ConstructionMaterials, "วัสดุก่อสร้างและอุปกรณ์การช่าง", ProductCategoryKind.Hardware));
    }

    [Fact]
    public async Task SeedAsync_ForMinimart_ShouldSeedTheExactLegacyLabels()
    {
        // MimyMart's legacy ids 10-19. Id 20 (การเกษตร) is deliberately absent: 0 products and
        // 0 invoice lines in the real database.
        await AssertSeededRowsAsync("STORE-LABELS-MM", StoreType.Minimart,
            (ProductCategoryCodes.Miscellaneous, "เบ็ดเตล็ด", ProductCategoryKind.GeneralGoods),
            (ProductCategoryCodes.Beverages, "เครื่องดื่ม", ProductCategoryKind.GeneralGoods),
            (ProductCategoryCodes.Snacks, "ขนม", ProductCategoryKind.GeneralGoods),
            (ProductCategoryCodes.AlcoholicBeverages, "เครื่องดื่มแอลกอฮอล์", ProductCategoryKind.GeneralGoods),
            (ProductCategoryCodes.Food, "อาหาร", ProductCategoryKind.GeneralGoods),
            (ProductCategoryCodes.Stationery, "เครื่องเขียน", ProductCategoryKind.GeneralGoods),
            (ProductCategoryCodes.Household, "ของใช้ในบ้าน", ProductCategoryKind.GeneralGoods),
            (ProductCategoryCodes.ElectricalAppliances, "เครื่องใช้ไฟฟ้า", ProductCategoryKind.GeneralGoods),
            (ProductCategoryCodes.Toys, "ของเล่น", ProductCategoryKind.GeneralGoods),
            (ProductCategoryCodes.Medicine, "ยา", ProductCategoryKind.GeneralGoods));
    }

    [Fact]
    public async Task SeedAsync_ForMimyShop_ShouldSeedTheExactLegacyLabels()
    {
        // MimyShop's legacy ids 10-26. Note these REUSE the same id range as MimyMart with
        // entirely different meanings — id 10 is ของขวัญ here but เบ็ดเตล็ด there.
        await AssertSeededRowsAsync("STORE-LABELS-MS", StoreType.MimyShop,
            (ProductCategoryCodes.Gifts, "ของขวัญ", ProductCategoryKind.GeneralGoods),
            (ProductCategoryCodes.Toys, "ของเล่น", ProductCategoryKind.GeneralGoods),
            (ProductCategoryCodes.Stationery, "เครื่องเขียน", ProductCategoryKind.GeneralGoods),
            (ProductCategoryCodes.BooksAndNotebooks, "หนังสือและสมุด", ProductCategoryKind.GeneralGoods),
            (ProductCategoryCodes.Cosmetics, "เครื่องสำอาง", ProductCategoryKind.GeneralGoods),
            (ProductCategoryCodes.Jewellery, "เครื่องประดับ", ProductCategoryKind.GeneralGoods),
            (ProductCategoryCodes.Bags, "กระเป๋า", ProductCategoryKind.GeneralGoods),
            (ProductCategoryCodes.Fashion, "แฟชั่น", ProductCategoryKind.GeneralGoods),
            (ProductCategoryCodes.Household, "ของใช้ในบ้าน", ProductCategoryKind.GeneralGoods),
            (ProductCategoryCodes.Kitchenware, "เครื่องครัว", ProductCategoryKind.GeneralGoods),
            (ProductCategoryCodes.SnacksAndBeverages, "ขนมและเครื่องดื่ม", ProductCategoryKind.GeneralGoods),
            (ProductCategoryCodes.MobileAccessories, "อุปกรณ์มือถือ", ProductCategoryKind.GeneralGoods),
            (ProductCategoryCodes.Electronics, "อุปกรณ์อิเล็กทรอนิกส์", ProductCategoryKind.GeneralGoods),
            (ProductCategoryCodes.PartySupplies, "อุปกรณ์งานปาร์ตี้", ProductCategoryKind.GeneralGoods),
            (ProductCategoryCodes.SeasonalGoods, "สินค้าตามเทศกาล", ProductCategoryKind.GeneralGoods),
            (ProductCategoryCodes.Services, "บริการ", ProductCategoryKind.Service),
            (ProductCategoryCodes.Miscellaneous, "เบ็ดเตล็ด", ProductCategoryKind.GeneralGoods));
    }

    [Fact]
    public async Task SeedAsync_ForMimyShop_ShouldSeedExactlyOneServiceAndNoHardware()
    {
        var identity = new MockStoreIdentityService
        {
            StoreId = "STORE-MS3", StoreType = StoreType.MimyShop
        };
        var repository = new ProductCategoryRepository(GetDbContext(), identity);

        await new ProductCategorySeeder(repository, identity,
            NullLogger<ProductCategorySeeder>.Instance).SeedAsync();

        var seeded = await repository.GetAllAsync();

        seeded.Count(c => c.Kind == ProductCategoryKind.Service).Should().Be(1, "only บริการ is a service");
        seeded.Should().NotContain(c => c.Kind == ProductCategoryKind.Hardware,
            "a gift shop sells no building materials");
    }

    [Fact]
    public async Task SeedAsync_ForMinimart_ShouldSeedNoHardwareOrServiceKind()
    {
        var identity = new MockStoreIdentityService
        {
            StoreId = "STORE-MM3", StoreType = StoreType.Minimart
        };
        var repository = new ProductCategoryRepository(GetDbContext(), identity);

        await new ProductCategorySeeder(repository, identity,
            NullLogger<ProductCategorySeeder>.Instance).SeedAsync();

        var seeded = await repository.GetAllAsync();

        seeded.Should().OnlyContain(c => c.Kind == ProductCategoryKind.GeneralGoods);
    }

    [Fact]
    public async Task SeedAsync_ForAnUnseededStoreType_ShouldThrowRatherThanLeaveAnEmptyCatalogue()
    {
        // An empty catalogue means no product can be created at all, so a missing seed table
        // must be a loud failure rather than a store that silently cannot add stock.
        var identity = new MockStoreIdentityService
        {
            StoreId = "STORE-UNKNOWN", StoreType = (StoreType)99
        };
        var seeder = new ProductCategorySeeder(
            new ProductCategoryRepository(GetDbContext(), identity), identity,
            NullLogger<ProductCategorySeeder>.Instance);

        var act = async () => await seeder.SeedAsync();

        await act.Should().ThrowAsync<ArgumentOutOfRangeException>();
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
