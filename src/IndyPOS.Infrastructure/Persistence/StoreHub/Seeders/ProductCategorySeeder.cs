using IndyPOS.Application.Abstractions.StoreHub.Repositories;
using IndyPOS.Application.Common.Constants;
using IndyPOS.Application.Common.Interfaces;
using IndyPOS.Domain.Entities.Core;
using IndyPOS.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace IndyPOS.Infrastructure.Persistence.StoreHub.Seeders;

/// <summary>
/// Seeds this store's product categories. Idempotent — only inserts a Code that is absent.
/// <para>Unlike <see cref="PaymentMethodSeeder"/>, the set depends on the STORE TYPE: the three
/// stores carry genuinely different catalogues, and legacy category ids collide across them
/// (id 10 is เบ็ดเตล็ด in GeneralHardware and ของขวัญ in MimyShop), so there is no shared default.</para>
/// </summary>
public class ProductCategorySeeder
{
    private readonly IProductCategoryRepository _repository;
    private readonly IStoreIdentityService _storeIdentity;
    private readonly ILogger<ProductCategorySeeder> _logger;

    public ProductCategorySeeder(IProductCategoryRepository repository, IStoreIdentityService storeIdentity,
        ILogger<ProductCategorySeeder> logger)
    {
        _repository = repository;
        _storeIdentity = storeIdentity;
        _logger = logger;
    }

    private sealed record Seed(string Code, string DisplayName, ProductCategoryKind Kind, int Order);

    // Shared by GeneralHardware and Minimart, in legacy id order 10-19.
    private static readonly Seed[] GroceryCommon =
    [
        new(ProductCategoryCodes.Miscellaneous, "เบ็ดเตล็ด", ProductCategoryKind.GeneralGoods, 1),
        new(ProductCategoryCodes.Beverages, "เครื่องดื่ม", ProductCategoryKind.GeneralGoods, 2),
        new(ProductCategoryCodes.Snacks, "ขนม", ProductCategoryKind.GeneralGoods, 3),
        new(ProductCategoryCodes.AlcoholicBeverages, "เครื่องดื่มแอลกอฮอล์", ProductCategoryKind.GeneralGoods, 4),
        new(ProductCategoryCodes.Food, "อาหาร", ProductCategoryKind.GeneralGoods, 5),
        new(ProductCategoryCodes.Stationery, "เครื่องเขียน", ProductCategoryKind.GeneralGoods, 6),
        new(ProductCategoryCodes.Household, "ของใช้ในบ้าน", ProductCategoryKind.GeneralGoods, 7),
        new(ProductCategoryCodes.ElectricalAppliances, "เครื่องใช้ไฟฟ้า", ProductCategoryKind.GeneralGoods, 8),
        new(ProductCategoryCodes.Toys, "ของเล่น", ProductCategoryKind.GeneralGoods, 9),
        new(ProductCategoryCodes.Medicine, "ยา", ProductCategoryKind.GeneralGoods, 10)
    ];

    // GeneralHardware = the grocery set, plus การเกษตร (legacy 20) and the five วัสดุ* ranges (50-54).
    private static readonly Seed[] GeneralHardwareSeeds =
    [
        .. GroceryCommon,
        new(ProductCategoryCodes.Agriculture, "การเกษตร", ProductCategoryKind.GeneralGoods, 11),
        new(ProductCategoryCodes.GeneralMaterials, "วัสดุและอุปกรณ์ทั่วไป", ProductCategoryKind.Hardware, 12),
        new(ProductCategoryCodes.MaterialsAndEquipment, "วัสดุและอุปกรณ์", ProductCategoryKind.Hardware, 13),
        new(ProductCategoryCodes.PlumbingMaterials, "วัสดุและอุปกรณ์ระบบประปา", ProductCategoryKind.Hardware, 14),
        new(ProductCategoryCodes.ElectricalMaterials, "วัสดุและอุปกรณ์ระบบไฟฟ้า", ProductCategoryKind.Hardware, 15),
        new(ProductCategoryCodes.ConstructionMaterials, "วัสดุก่อสร้างและอุปกรณ์การช่าง", ProductCategoryKind.Hardware, 16)
    ];

    // Minimart = the grocery set only. การเกษตร is deliberately absent: it was copied from
    // GeneralHardware and has 0 products and 0 invoice lines in the real MimyMart database.
    private static readonly Seed[] MinimartSeeds = [.. GroceryCommon];

    // MimyShop reuses legacy ids 10-26 with entirely different meanings. All 17 are seeded even
    // though 12 currently have no products: it is a new store still adding inventory.
    private static readonly Seed[] MimyShopSeeds =
    [
        new(ProductCategoryCodes.Gifts, "ของขวัญ", ProductCategoryKind.GeneralGoods, 1),
        new(ProductCategoryCodes.Toys, "ของเล่น", ProductCategoryKind.GeneralGoods, 2),
        new(ProductCategoryCodes.Stationery, "เครื่องเขียน", ProductCategoryKind.GeneralGoods, 3),
        new(ProductCategoryCodes.BooksAndNotebooks, "หนังสือและสมุด", ProductCategoryKind.GeneralGoods, 4),
        new(ProductCategoryCodes.Cosmetics, "เครื่องสำอาง", ProductCategoryKind.GeneralGoods, 5),
        new(ProductCategoryCodes.Jewellery, "เครื่องประดับ", ProductCategoryKind.GeneralGoods, 6),
        new(ProductCategoryCodes.Bags, "กระเป๋า", ProductCategoryKind.GeneralGoods, 7),
        new(ProductCategoryCodes.Fashion, "แฟชั่น", ProductCategoryKind.GeneralGoods, 8),
        new(ProductCategoryCodes.Household, "ของใช้ในบ้าน", ProductCategoryKind.GeneralGoods, 9),
        new(ProductCategoryCodes.Kitchenware, "เครื่องครัว", ProductCategoryKind.GeneralGoods, 10),
        new(ProductCategoryCodes.SnacksAndBeverages, "ขนมและเครื่องดื่ม", ProductCategoryKind.GeneralGoods, 11),
        new(ProductCategoryCodes.MobileAccessories, "อุปกรณ์มือถือ", ProductCategoryKind.GeneralGoods, 12),
        new(ProductCategoryCodes.Electronics, "อุปกรณ์อิเล็กทรอนิกส์", ProductCategoryKind.GeneralGoods, 13),
        new(ProductCategoryCodes.PartySupplies, "อุปกรณ์งานปาร์ตี้", ProductCategoryKind.GeneralGoods, 14),
        new(ProductCategoryCodes.SeasonalGoods, "สินค้าตามเทศกาล", ProductCategoryKind.GeneralGoods, 15),
        new(ProductCategoryCodes.Services, "บริการ", ProductCategoryKind.Service, 16),
        new(ProductCategoryCodes.Miscellaneous, "เบ็ดเตล็ด", ProductCategoryKind.GeneralGoods, 17)
    ];

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        var storeType = _storeIdentity.StoreType;
        var seeds = SeedsFor(storeType);
        var now = DateTime.UtcNow;
        var seededCount = 0;

        foreach (var seed in seeds)
        {
            if (await _repository.GetByCodeAsync(seed.Code, cancellationToken) is not null) continue;

            await _repository.AddAsync(new ProductCategory
            {
                Code = seed.Code,
                DisplayName = seed.DisplayName,
                Kind = seed.Kind,
                IsEnabled = true,
                DisplayOrder = seed.Order,
                StoreId = _storeIdentity.StoreId,
                CreatedUtc = now,
                LastModifiedUtc = now
            }, cancellationToken);
            seededCount++;
        }

        _logger.LogInformation(
            "Product category seeding complete for {StoreType}: {SeededCount} of {TotalCount} inserted",
            storeType, seededCount, seeds.Length);
    }

    private static Seed[] SeedsFor(StoreType storeType) => storeType switch
    {
        StoreType.GeneralHardware => GeneralHardwareSeeds,
        StoreType.Minimart => MinimartSeeds,
        StoreType.MimyShop => MimyShopSeeds,
        // A new store type with no seed table must be a loud failure, not an empty catalogue:
        // an empty catalogue means no product can be created at all.
        _ => throw new ArgumentOutOfRangeException(nameof(storeType), storeType,
            "No product-category seed set is defined for this store type.")
    };
}
