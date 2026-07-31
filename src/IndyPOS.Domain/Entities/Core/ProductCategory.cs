using IndyPOS.Domain.Enums;

namespace IndyPOS.Domain.Entities.Core;

/// <summary>
/// A product category available in this store. Rows are data (not a hardcoded enum) because the
/// three store types carry genuinely different category sets, and legacy category ids collide
/// across them — id 10 is เบ็ดเตล็ด (misc) in GeneralHardware and ของขวัญ (gifts) in MimyShop.
/// <para>Code is the stable key stored on <see cref="Product.Category"/> and used in reports.
/// Codes are shared across stores where the meaning genuinely matches, which is what makes
/// cross-store reporting a plain GROUP BY.</para>
/// </summary>
public class ProductCategory
{
    public string StoreId { get; set; } = default!;
    public string Code { get; set; } = default!;
    public string DisplayName { get; set; } = default!;
    public ProductCategoryKind Kind { get; set; }
    public bool IsEnabled { get; set; }
    public int DisplayOrder { get; set; }
    public DateTime CreatedUtc { get; set; }
    public DateTime LastModifiedUtc { get; set; }
}
