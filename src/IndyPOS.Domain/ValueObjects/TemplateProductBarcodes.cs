namespace IndyPOS.Domain.ValueObjects;

/// <summary>
/// The open-price products behind the sale panel's เบ็ดเตล็ด and ฮาร์ดแวร์ buttons: the cashier types
/// the price of an unlabelled item at the till. The barcodes are the real stores' own (every store has
/// GeneralGoods; only GeneralHardware has Hardware), so tills, migrations and dev seeds keep them verbatim.
/// </summary>
public static class TemplateProductBarcodes
{
    public const string GeneralGoods = "2001000000012";  // สินค้าเบ็ดเตล็ด
    public const string Hardware = "2005000000027";      // สินค้าฮาร์ดแวร์
}
