namespace IndyPOS.Application.Abstractions.StoreHub;

/// <summary>
/// The catalog's display name for each payment-method code, e.g. "Cash" → "เงินสด". Keys ignore
/// case. A code the catalog does not know is absent, so callers fall back to the code itself
/// (<c>names.GetValueOrDefault(code, code)</c>) and never index a payment by it.
/// </summary>
public interface IPaymentMethodNames
{
    Task<IReadOnlyDictionary<string, string>> GetAsync(CancellationToken cancellationToken = default);
}
