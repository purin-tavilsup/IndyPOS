namespace IndyPOS.Application.Common.Models;

/// <summary>
/// Which of the till's optional pieces this store has, decided from its store-type features alone.
/// The forms only apply it (WinForms today, Avalonia later).
/// </summary>
public sealed record TillLayout(bool ShowHardwareButton, bool ShowServiceButtons, bool ShowAccountsReceivableMenu)
{
    /// <summary>
    /// The till as designed, for when the store's features cannot be fetched: the hardware button and the
    /// ledger menu stay (StoreHub still refuses what this store may not do), and the service buttons stay off.
    /// </summary>
    public static TillLayout WhenFeaturesUnavailable { get; } = new(
        ShowHardwareButton: true,
        ShowServiceButtons: false,
        ShowAccountsReceivableMenu: true);

    public static TillLayout For(StoreFeaturesDto features) => new(
        ShowHardwareButton: features.MultipleProductTypesEnabled,
        ShowServiceButtons: features.ServiceProductsEnabled,
        ShowAccountsReceivableMenu: features.PayLaterEnabled);
}
