using IndyPOS.Infrastructure.Persistence.StoreHub;
using Microsoft.EntityFrameworkCore;

namespace IndyPOS.Infrastructure.QueryHandlers.Sales;

/// <summary>
/// Display names for the payment methods stored on bills, from the store's whole catalogue: disabled
/// campaign methods (ม.33, เราชนะ) are kept there precisely so their old bills still have a name.
/// </summary>
internal static class PaymentMethodCatalogue
{
    /// <remarks>
    /// Matched ignoring case, as CompleteSaleCommandHandler validates codes: it stores the caller's
    /// spelling, so "cash" is a valid stored code against the catalogue's "Cash". The whole store
    /// catalogue is loaded (about ten rows) because a SQL IN would compare case-sensitively.
    /// </remarks>
    public static async Task<Dictionary<string, string>> NamesAsync(StoreHubDbContext db, string storeId,
                                                                   CancellationToken cancellationToken)
    {
        var catalogue = await db.PaymentMethods
                                .AsNoTracking()
                                .Where(m => m.StoreId == storeId)
                                .Select(m => new { m.Code, m.DisplayName })
                                .ToListAsync(cancellationToken);

        // The catalogue key is case-sensitive, so "Cash" and "cash" can both exist; the first in
        // ordinal order wins rather than letting a duplicate hide every bill.
        var names = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var method in catalogue.OrderBy(m => m.Code, StringComparer.Ordinal))
            names.TryAdd(method.Code, method.DisplayName);

        return names;
    }
}
