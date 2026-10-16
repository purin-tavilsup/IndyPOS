using IndyPOS.Application.Common.Models;
using Serilog;

namespace IndyPOS.Windows.Forms.Services;

public static class StoreFeaturesProviderExtensions
{
    /// <summary>
    /// The store's till layout, or the designed layout when the features cannot be fetched: a screen still
    /// opens, and StoreHub still refuses whatever this store may not do.
    /// </summary>
    public static async Task<TillLayout> GetLayoutAsync(this IStoreFeaturesProvider features, string screen)
    {
        try
        {
            return TillLayout.For(await features.GetAsync());
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Could not load store features for {Screen}", screen);
            return TillLayout.WhenFeaturesUnavailable;
        }
    }
}
