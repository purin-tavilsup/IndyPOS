using IndyPOS.Domain.Enums;

namespace IndyPOS.StoreProfiles;

public static class StoreProfiles
{
    public const string AllKey = "all";

    /// <summary>
    /// The secret every dev store uses to sign in to the dev cloud. Registered only by CloudApi's
    /// Development seeder; production stores get a random one from /admin/stores/register.
    /// </summary>
    public const string DevCloudClientSecret = "dev-store-secret-not-for-production";

    public static StoreProfile GeneralHardware { get; } = new(
        "GeneralHardware", StoreType.GeneralHardware, "DEV-GENERALHARDWARE",
        "ร้านวัสดุ (Dev)", "ร้านวัสดุก่อสร้าง ทดสอบ", "123 ถนนทดสอบ", "เมือง 10000", "000-000-0001",
        Code: 1, DevPort: 5012,
        Products:
        [
            new("8850100000011", "ปูนซีเมนต์ 50 กก.", "ConstructionMaterials", 145m, 40),
            new("8850100000028", "ท่อ PVC 1/2 นิ้ว 4 ม.", "PlumbingMaterials", 65m, 60),
            new("8850100000035", "สายไฟ VAF 2x1.5 (เมตร)", "ElectricalMaterials", 18m, 300),
            new("8850100000042", "ตะปู 2 นิ้ว (กก.)", "GeneralMaterials", 55m, 50),
            new("8850100000059", "ปุ๋ยยูเรีย 50 กก.", "Agriculture", 890m, 20),
            new("8850000000001", "น้ำดื่ม 600ml", "Beverages", 7m, 100)
        ]);

    public static StoreProfile MimyMart { get; } = new(
        "MimyMart", StoreType.Minimart, "DEV-MIMYMART",
        "มินิมาร์ท (Dev)", "มินิมาร์ท ทดสอบ", "456 ถนนทดสอบ", "เมือง 10000", "000-000-0002",
        Code: 2, DevPort: 5013,
        Products:
        [
            new("8850000000001", "น้ำดื่ม 600ml", "Beverages", 7m, 120),
            new("8850000000002", "โค้ก 325ml", "Beverages", 15m, 80),
            new("8850000000003", "มาม่าหมูสับ", "Food", 6m, 200),
            new("8850000000004", "ขนมปังปี๊บ", "Snacks", 20m, 30),
            new("8850000000005", "นมจืด 200ml", "Beverages", 12m, 60),
            new("8850200000016", "ผงซักฟอก 800 ก.", "Household", 45m, 25)
        ]);

    public static StoreProfile MimyShop { get; } = new(
        "MimyShop", StoreType.MimyShop, "DEV-MIMYSHOP",
        "มิมี่ช็อป (Dev)", "มิมี่ช็อป ทดสอบ", "789 ถนนทดสอบ", "เมือง 10000", "000-000-0003",
        Code: 3, DevPort: 5014,
        Products:
        [
            new("2002500000014", "จัดส่ง", "Services", 30m, 0, IsTrackable: false),
            new("2002500000021", "เอกสาร", "Services", 10m, 0, IsTrackable: false),
            new("8850300000013", "สมุดปกอ่อน", "BooksAndNotebooks", 20m, 50),
            new("8850300000020", "ปากกาลูกลื่น", "Stationery", 10m, 100),
            new("8850300000037", "ตุ๊กตาหมี", "Toys", 159m, 12),
            new("8850300000044", "สายชาร์จ USB-C", "MobileAccessories", 99m, 30)
        ]);

    public static IReadOnlyList<StoreProfile> All { get; } = [GeneralHardware, MimyMart, MimyShop];

    public static StoreProfile Default => GeneralHardware;

    public static StoreProfile? Find(string? key) =>
        All.FirstOrDefault(p => string.Equals(p.Key, key?.Trim(), StringComparison.OrdinalIgnoreCase));

    public static StoreProfile ForType(StoreType type) => All.FirstOrDefault(p => p.Type == type) ?? Default;

    /// <summary>The stores a dev run starts: none chosen = the default, "all" = every store.</summary>
    public static IReadOnlyList<StoreProfile> Resolve(string? selection)
    {
        if (string.IsNullOrWhiteSpace(selection))
            return [Default];

        if (string.Equals(selection.Trim(), AllKey, StringComparison.OrdinalIgnoreCase))
            return All;

        return Find(selection) is { } profile
            ? [profile]
            : throw new ArgumentException(
                $"Unknown store '{selection.Trim()}'. Use one of: {string.Join(", ", All.Select(p => p.Key))}, {AllKey}.",
                nameof(selection));
    }
}
