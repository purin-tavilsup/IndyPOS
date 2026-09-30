using IndyPOS.Application.Common.Constants;
using IndyPOS.Application.Common.Exceptions;

namespace IndyPOS.Application.UseCases.StoreHub.Sales.Complete;

/// <summary>
/// What a sale's payments must satisfy before anything is saved (spec 2026-09-30 §4.2). A PayLater
/// sale is paid wholly on credit: never mixed with another method, because a mixed bill cannot be
/// split honestly between general goods and hardware, and never on a refund, which would record a
/// debt for money the store owes.
/// </summary>
public static class SalePaymentRules
{
    /// <summary>PayLater.Description's column length. Payment.Note has no limit.</summary>
    public const int MaxCustomerNameLength = 500;

    public static void EnsureValid(
        IReadOnlyList<SalePaymentRequest> payments, decimal invoiceTotal, IReadOnlySet<string> offerableCodes)
    {
        var notOfferable = payments.FirstOrDefault(p => !offerableCodes.Contains(p.Method));
        if (notOfferable is not null)
            throw new SaleValidationException($"ช่องทางชำระเงิน '{notOfferable.Method}' ใช้กับร้านนี้ไม่ได้");

        if (payments.Any(p => IsPayLater(p.Method)))
            EnsureWhollyOnCredit(payments, invoiceTotal);
    }

    /// <summary>
    /// Ignoring case: the handler accepts "paylater" as offerable and stores the caller's spelling.
    /// </summary>
    public static bool IsPayLater(string method) =>
        string.Equals(method, PaymentMethodCodes.PayLater, StringComparison.OrdinalIgnoreCase);

    private static void EnsureWhollyOnCredit(IReadOnlyList<SalePaymentRequest> payments, decimal invoiceTotal)
    {
        if (payments.Count > 1)
            throw new SaleValidationException("การลงบัญชีต้องไม่รวมกับการชำระแบบอื่น");

        EnsureNamedCustomer(payments[0].Note);

        if (invoiceTotal <= 0)
            throw new SaleValidationException("ไม่สามารถลงบัญชีบิลคืนสินค้าได้");

        if (payments[0].Amount <= 0)
            throw new SaleValidationException("ยอดลงบัญชีต้องมากกว่า 0");

        if (payments[0].Amount != invoiceTotal)
            throw new SaleValidationException("ยอดลงบัญชีต้องเท่ากับยอดบิล");
    }

    private static void EnsureNamedCustomer(string? note)
    {
        var name = note?.Trim();

        if (string.IsNullOrEmpty(name))
            throw new SaleValidationException("กรุณาใส่ชื่อลูกค้าสำหรับการลงบัญชี");

        // Counted as PostgreSQL's varchar counts: one per character, where string.Length counts an
        // emoji twice (two UTF-16 code units) and would refuse a name the column accepts.
        if (name.EnumerateRunes().Count() > MaxCustomerNameLength)
            throw new SaleValidationException($"ชื่อลูกค้ายาวเกิน {MaxCustomerNameLength} ตัวอักษร");
    }
}
