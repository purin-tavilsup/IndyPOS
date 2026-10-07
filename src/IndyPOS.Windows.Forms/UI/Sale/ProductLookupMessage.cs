using IndyPOS.Application.Common.Exceptions;

namespace IndyPOS.Windows.Forms.UI.Sale;

/// <summary>What the cashier reads when looking a product up by barcode fails.</summary>
public static class ProductLookupMessage
{
    // A missing product needs no English detail; any other failure keeps it, so its cause can be told apart.
    public static string For(string barcode, Exception error) => error switch
    {
        ProductNotFoundException => $"ไม่พบรหัสสินค้า {barcode} ในระบบ",
        _ => $"ไม่พบรหัสสินค้า {barcode} ในระบบ Error: {error.Message}"
    };
}
