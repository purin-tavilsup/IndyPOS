using IndyPOS.Application.Common.Models;

namespace IndyPOS.Windows.Forms.Services;

/// <summary>The store's feature flags, fetched from StoreHub once per run of the till.</summary>
public interface IStoreFeaturesProvider
{
    Task<StoreFeaturesDto> GetAsync();
}
