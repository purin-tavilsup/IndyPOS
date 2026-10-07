using IndyPOS.Application.Abstractions.StoreHub;
using IndyPOS.Application.Common.Models;

namespace IndyPOS.Windows.Forms.Services;

/// <summary>
/// Fetches the store's features once and shares the answer: the store type cannot change while the till
/// runs, and the menu and the sale panel both ask on every login. A failed fetch is forgotten, so the next
/// caller asks StoreHub again.
/// </summary>
public sealed class StoreFeaturesProvider(IStoreHubClient storeHubClient) : IStoreFeaturesProvider
{
    private readonly Lock _gate = new();
    private Task<StoreFeaturesDto>? _features;

    public async Task<StoreFeaturesDto> GetAsync()
    {
        Task<StoreFeaturesDto> features;
        lock (_gate)
            features = _features ??= storeHubClient.GetStoreFeaturesAsync();

        try
        {
            return await features;
        }
        catch
        {
            ForgetFailed(features);
            throw;
        }
    }

    private void ForgetFailed(Task<StoreFeaturesDto> failed)
    {
        lock (_gate)
        {
            if (_features == failed)
                _features = null;
        }
    }
}
