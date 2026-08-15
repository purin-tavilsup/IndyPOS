using System.Data.SQLite;
using Dapper;

namespace IndyPOS.MigrationTool.Tests.Tools;

/// <summary>
/// Seeds the legacy <c>ProductCategory</c> lookup, which every real store ships with.
/// </summary>
/// <remarks>
/// Seeded by <c>LegacyStoreDatabase.CreateAsync</c> alongside the DDL, because it is reference
/// data the store schema comes with rather than per-test data. A fixture without it would let a
/// category-resolution bug pass here while every real store failed.
/// <para>Rows are the real ones. MimyMart shares MimyShop's SCHEMA but carries the grocery names,
/// so its ids look like GeneralHardware's -- which is exactly why the mapping keys on the name.</para>
/// </remarks>
internal static class LegacyCategoryLookup
{
    private static readonly (int Id, string Name)[] GeneralHardware =
    [
        (10, "เบ็ดเตล็ด"),
        (11, "เครื่องดื่ม"),
        (12, "ขนม"),
        (13, "เครื่องดื่มแอลกอฮอล์"),
        (14, "อาหาร"),
        (15, "เครื่องเขียน"),
        (16, "ของใช้ในบ้าน"),
        (17, "เครื่องใช้ไฟฟ้า"),
        (18, "ของเล่น"),
        (19, "ยา"),
        (20, "การเกษตร"),
        (50, "วัสดุและอุปกรณ์ทั่วไป"),
        (51, "วัสดุและอุปกรณ์"),
        (52, "วัสดุและอุปกรณ์ระบบประปา"),
        (53, "วัสดุและอุปกรณ์ระบบไฟฟ้า"),
        (54, "วัสดุก่อสร้างและอุปกรณ์การช่าง")
    ];

    private static readonly (int Id, string Name)[] MimyShop =
    [
        (10, "ของขวัญ"),
        (11, "ของเล่น"),
        (12, "เครื่องเขียน"),
        (13, "หนังสือและสมุด"),
        (14, "เครื่องสำอาง"),
        (15, "เครื่องประดับ"),
        (16, "กระเป๋า"),
        (17, "แฟชั่น"),
        (18, "ของใช้ในบ้าน"),
        (19, "เครื่องครัว"),
        (20, "ขนมและเครื่องดื่ม"),
        (21, "อุปกรณ์มือถือ"),
        (22, "อุปกรณ์อิเล็กทรอนิกส์"),
        (23, "อุปกรณ์งานปาร์ตี้"),
        (24, "สินค้าตามเทศกาล"),
        (25, "บริการ"),
        (26, "เบ็ดเตล็ด")
    ];

    public static async Task SeedAsync(SQLiteConnection connection, LegacyStoreShape shape)
    {
        var rows = shape == LegacyStoreShape.MimyShop ? MimyShop : GeneralHardware;

        foreach (var (id, name) in rows)
        {
            await connection.ExecuteAsync(
                "INSERT INTO ProductCategory (Id, Category) VALUES (@id, @name)", new { id, name });
        }
    }
}
