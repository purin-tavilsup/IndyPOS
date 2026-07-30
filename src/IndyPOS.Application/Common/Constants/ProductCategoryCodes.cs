namespace IndyPOS.Application.Common.Constants;

/// <summary>
/// Stable product-category codes. Stored on <c>Product.Category</c> and mapped to by the
/// SQLite migration, so a value here is a data contract — rename nothing without a migration.
/// <para>Codes are shared across stores where the meaning genuinely matches (Toys, Stationery,
/// Household, Miscellaneous), which is what makes cross-store reporting a plain GROUP BY.</para>
/// </summary>
public static class ProductCategoryCodes
{
    // --- General goods, present in GeneralHardware and MimyMart ---
    public const string Miscellaneous = "Miscellaneous";              // เบ็ดเตล็ด
    public const string Beverages = "Beverages";                      // เครื่องดื่ม
    public const string Snacks = "Snacks";                            // ขนม
    public const string AlcoholicBeverages = "AlcoholicBeverages";    // เครื่องดื่มแอลกอฮอล์
    public const string Food = "Food";                                // อาหาร
    public const string Stationery = "Stationery";                    // เครื่องเขียน
    public const string Household = "Household";                      // ของใช้ในบ้าน
    public const string ElectricalAppliances = "ElectricalAppliances";// เครื่องใช้ไฟฟ้า
    public const string Toys = "Toys";                                // ของเล่น
    public const string Medicine = "Medicine";                        // ยา

    // --- GeneralHardware only ---
    public const string Agriculture = "Agriculture";                          // การเกษตร
    public const string GeneralMaterials = "GeneralMaterials";                // วัสดุและอุปกรณ์ทั่วไป
    public const string MaterialsAndEquipment = "MaterialsAndEquipment";      // วัสดุและอุปกรณ์
    public const string PlumbingMaterials = "PlumbingMaterials";              // วัสดุและอุปกรณ์ระบบประปา
    public const string ElectricalMaterials = "ElectricalMaterials";          // วัสดุและอุปกรณ์ระบบไฟฟ้า
    public const string ConstructionMaterials = "ConstructionMaterials";      // วัสดุก่อสร้างและอุปกรณ์การช่าง

    // --- MimyShop only ---
    public const string Gifts = "Gifts";                              // ของขวัญ
    public const string BooksAndNotebooks = "BooksAndNotebooks";      // หนังสือและสมุด
    public const string Cosmetics = "Cosmetics";                      // เครื่องสำอาง
    public const string Jewellery = "Jewellery";                      // เครื่องประดับ
    public const string Bags = "Bags";                                // กระเป๋า
    public const string Fashion = "Fashion";                          // แฟชั่น
    public const string Kitchenware = "Kitchenware";                  // เครื่องครัว
    public const string SnacksAndBeverages = "SnacksAndBeverages";    // ขนมและเครื่องดื่ม
    public const string MobileAccessories = "MobileAccessories";      // อุปกรณ์มือถือ
    public const string Electronics = "Electronics";                  // อุปกรณ์อิเล็กทรอนิกส์
    public const string PartySupplies = "PartySupplies";              // อุปกรณ์งานปาร์ตี้
    public const string SeasonalGoods = "SeasonalGoods";              // สินค้าตามเทศกาล
    public const string Services = "Services";                        // บริการ
}
