using IndyPOS.Application.Abstractions.StoreHub;
using Microsoft.Extensions.Logging;

namespace IndyPOS.Infrastructure.Services.StoreHub;

/// <summary>
/// Loads the offerable catalog (GET /payment-methods) once. Cashiers cannot read the full catalog,
/// and every payment the till takes comes from an offerable button, so this covers every code a
/// sale holds. A failed load is not cached: the next call tries again.
/// </summary>
public sealed class StoreHubPaymentMethodNames(IStoreHubClient storeHubClient, ILogger<StoreHubPaymentMethodNames> logger)
    : IPaymentMethodNames
{
    private static readonly IReadOnlyDictionary<string, string> NoNames =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

    private IReadOnlyDictionary<string, string>? _names;

    public async Task<IReadOnlyDictionary<string, string>> GetAsync(CancellationToken cancellationToken = default)
    {
        if (_names is not null)
            return _names;

        try
        {
            var methods = await storeHubClient.GetOfferablePaymentMethodsAsync(cancellationToken);

            return _names = methods.ToDictionary(m => m.Code, m => m.DisplayName, StringComparer.OrdinalIgnoreCase);
        }
        // Only the caller's own cancellation propagates. HttpClient.Timeout is a TaskCanceledException too,
        // and it is just a failed load.
        catch (Exception ex) when (!cancellationToken.IsCancellationRequested)
        {
            logger.LogWarning(ex, "Payment-method names could not be loaded; showing raw codes until the next try");

            return NoNames;
        }
    }
}
