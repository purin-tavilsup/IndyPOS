using IndyPOS.Application.Common.Constants;

namespace IndyPOS.MigrationTool.Services;

/// <summary>
/// Maps a legacy category's Thai name to the catalogue <c>Code</c> Epic 1 introduced.
/// </summary>
/// <remarks>
/// <para>
/// Keyed by NAME, not by the legacy id, because the id is not a stable key: it means different
/// things in different stores. Id 10 is เบ็ดเตล็ด in GeneralHardware and MimyMart but ของขวัญ in
/// MimyShop, and id 18 is ของเล่น in the first two and ของใช้ในบ้าน in the third. Each store's own
/// <c>ProductCategory</c> table resolves its ids to names locally, so this map never has to know
/// which store it is looking at.
/// </para>
/// <para>
/// Verified against all three real stores: 44 legacy category rows, every one matching a seeded
/// name, and no name mapping to two different codes. Mirrors <see cref="LegacyPaymentTypeMap"/> --
/// an unmapped value returns null and is reported, never guessed.
/// </para>
/// </remarks>
public static class LegacyCategoryMap
{
    private static readonly Dictionary<string, string> CodeByName = new()
    {
        ["การเกษตร"                      ] = ProductCategoryCodes.Agriculture,
        ["เครื่องดื่มแอลกอฮอล์"          ] = ProductCategoryCodes.AlcoholicBeverages,
        ["กระเป๋า"                       ] = ProductCategoryCodes.Bags,
        ["เครื่องดื่ม"                   ] = ProductCategoryCodes.Beverages,
        ["หนังสือและสมุด"                ] = ProductCategoryCodes.BooksAndNotebooks,
        ["วัสดุก่อสร้างและอุปกรณ์การช่าง"] = ProductCategoryCodes.ConstructionMaterials,
        ["เครื่องสำอาง"                  ] = ProductCategoryCodes.Cosmetics,
        ["เครื่องใช้ไฟฟ้า"               ] = ProductCategoryCodes.ElectricalAppliances,
        ["วัสดุและอุปกรณ์ระบบไฟฟ้า"      ] = ProductCategoryCodes.ElectricalMaterials,
        ["อุปกรณ์อิเล็กทรอนิกส์"         ] = ProductCategoryCodes.Electronics,
        ["แฟชั่น"                        ] = ProductCategoryCodes.Fashion,
        ["อาหาร"                         ] = ProductCategoryCodes.Food,
        ["วัสดุและอุปกรณ์ทั่วไป"         ] = ProductCategoryCodes.GeneralMaterials,
        ["ของขวัญ"                       ] = ProductCategoryCodes.Gifts,
        ["ของใช้ในบ้าน"                  ] = ProductCategoryCodes.Household,
        ["เครื่องประดับ"                 ] = ProductCategoryCodes.Jewellery,
        ["เครื่องครัว"                   ] = ProductCategoryCodes.Kitchenware,
        ["วัสดุและอุปกรณ์"               ] = ProductCategoryCodes.MaterialsAndEquipment,
        ["ยา"                            ] = ProductCategoryCodes.Medicine,
        ["เบ็ดเตล็ด"                     ] = ProductCategoryCodes.Miscellaneous,
        ["อุปกรณ์มือถือ"                 ] = ProductCategoryCodes.MobileAccessories,
        ["อุปกรณ์งานปาร์ตี้"             ] = ProductCategoryCodes.PartySupplies,
        ["วัสดุและอุปกรณ์ระบบประปา"      ] = ProductCategoryCodes.PlumbingMaterials,
        ["สินค้าตามเทศกาล"               ] = ProductCategoryCodes.SeasonalGoods,
        ["บริการ"                        ] = ProductCategoryCodes.Services,
        ["ขนม"                           ] = ProductCategoryCodes.Snacks,
        ["ขนมและเครื่องดื่ม"             ] = ProductCategoryCodes.SnacksAndBeverages,
        ["เครื่องเขียน"                  ] = ProductCategoryCodes.Stationery,
        ["ของเล่น"                       ] = ProductCategoryCodes.Toys,
    };

    /// <returns>The catalogue code, or <c>null</c> if this name is not one the catalogue knows.</returns>
    public static string? ToCode(string? legacyCategoryName)
    {
        if (string.IsNullOrWhiteSpace(legacyCategoryName)) return null;

        return CodeByName.GetValueOrDefault(legacyCategoryName.Trim());
    }
}
