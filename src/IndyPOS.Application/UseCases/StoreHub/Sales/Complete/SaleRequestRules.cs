using IndyPOS.Application.Common.Exceptions;

namespace IndyPOS.Application.UseCases.StoreHub.Sales.Complete;

/// <summary>
/// The shape a sale must have before any rule can read it. System.Text.Json leaves a list the body
/// omits, or an item written as null, as null despite the non-nullable types, so they are checked
/// here rather than trusted. An empty list is a well-formed zero sale and passes.
/// </summary>
public static class SaleRequestRules
{
    public const string MalformedLines = "ข้อมูลรายการสินค้าไม่ถูกต้อง";
    public const string MalformedPayments = "ข้อมูลการชำระเงินไม่ถูกต้อง";

    public static void EnsureWellFormed(
        IReadOnlyList<SaleLineRequest?>? lines, IReadOnlyList<SalePaymentRequest?>? payments)
    {
        if (lines is null || lines.Any(l => l is null))
            throw new SaleValidationException(MalformedLines);

        if (payments is null || payments.Any(p => p is null))
            throw new SaleValidationException(MalformedPayments);
    }
}
