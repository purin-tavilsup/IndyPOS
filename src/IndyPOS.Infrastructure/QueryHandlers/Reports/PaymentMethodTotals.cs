using IndyPOS.Application.UseCases.StoreHub.Reports;
using IndyPOS.Domain.Entities.Core;

namespace IndyPOS.Infrastructure.QueryHandlers.Reports;

/// <summary>
/// Takings per payment method for a report. A method shows when the store offers it, or when it was
/// used in the range: a deprecated campaign (เราชนะ) still shows in the months it ran.
/// </summary>
public static class PaymentMethodTotals
{
    public static IReadOnlyList<PaymentMethodTotalDto> Build(IReadOnlyList<PaymentMethod> catalogue,
                                                             IReadOnlyList<Payment> payments)
    {
        var totals = payments.GroupBy(p => p.Method)
                             .ToDictionary(g => g.Key, g => g.Sum(p => p.Amount));

        var known = catalogue.Where(m => m.IsEnabled || totals.ContainsKey(m.Code))
                             .OrderBy(m => m.DisplayOrder)
                             .Select(m => new PaymentMethodTotalDto(m.Code, m.DisplayName, totals.GetValueOrDefault(m.Code)));

        // A code no catalogue row names (an old "Card", say) still counts, under its own code, last.
        var catalogueCodes = catalogue.Select(m => m.Code).ToHashSet();
        var unknown = totals.Where(t => !catalogueCodes.Contains(t.Key))
                            .OrderBy(t => t.Key, StringComparer.Ordinal)
                            .Select(t => new PaymentMethodTotalDto(t.Key, t.Key, t.Value));

        return known.Concat(unknown).ToList();
    }
}
