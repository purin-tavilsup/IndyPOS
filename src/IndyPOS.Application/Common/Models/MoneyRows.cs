using IndyPOS.Application.UseCases.StoreHub.Reports;

namespace IndyPOS.Application.Common.Models;

/// <summary>One line of the overview's money section: a payment method's takings, or a service's sales.</summary>
public sealed record MoneyRow(string Title, decimal Amount);

/// <summary>The overview's money section, in the spec's order: payment methods, then services.</summary>
public static class MoneyRows
{
    public static IReadOnlyList<MoneyRow> From(SalesSummaryDto summary) =>
        summary.PaymentsByMethod.Select(m => new MoneyRow(m.DisplayName, m.Total))
               .Concat(summary.ServiceSales.Select(s => new MoneyRow(s.Name, s.Total)))
               .ToList();
}
