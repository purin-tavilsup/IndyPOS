using IndyPOS.Application.Common.Constants;

namespace IndyPOS.Application.UseCases.StoreHub.Sales.History;

public readonly record struct SalePayment(string Method, decimal Amount);

/// <summary>
/// The money a receipt prints that the invoice row does not store. Derived with the same rules the
/// till used when it printed the original (StoreHubSaleService.CalculateChanges /
/// IsRefundInvoice), so a reprint shows the same figures.
/// </summary>
public readonly record struct SaleFigures(
    decimal AmountReceived,
    decimal ChangeGiven,
    bool IsRefund,
    bool HasPayLater,
    decimal PayLaterAmount)
{
    public static SaleFigures From(decimal totalAmount, IEnumerable<SalePayment> payments)
    {
        var all = payments.ToList();
        var received = all.Sum(p => p.Amount);
        var isRefund = totalAmount < 0;
        var payLater = all.Where(p => string.Equals(p.Method, PaymentMethodCodes.PayLater, StringComparison.OrdinalIgnoreCase))
                          .ToList();

        return new SaleFigures(
            AmountReceived: received,
            ChangeGiven: isRefund ? 0m : Math.Max(received - totalAmount, 0m),
            IsRefund: isRefund,
            HasPayLater: payLater.Count > 0,
            PayLaterAmount: payLater.Sum(p => p.Amount));
    }
}
