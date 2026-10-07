namespace IndyPOS.Application.Common.Models;

/// <summary>
/// Which of the till's optional pieces this store has, decided from its store-type features alone.
/// The forms only apply it (WinForms today, Avalonia later).
/// </summary>
/// <param name="ShowPayLaterReports">The outstanding ลงบัญชี report tab and the overview's ลงบัญชี tiles.</param>
/// <param name="ShowProductTypeSplit">The general/hardware tiles, and products sold's filter and category column.</param>
public sealed record TillLayout(
    bool ShowHardwareButton,
    bool ShowServiceButtons,
    bool ShowAccountsReceivableMenu,
    bool ShowPayLaterReports,
    bool ShowProductTypeSplit)
{
    /// <summary>
    /// The till as designed, for when the store's features cannot be fetched: the hardware button, the ledger
    /// menu and the report pieces stay (StoreHub still refuses what this store may not do), and the service
    /// buttons stay off.
    /// </summary>
    public static TillLayout WhenFeaturesUnavailable { get; } = new(
        ShowHardwareButton: true,
        ShowServiceButtons: false,
        ShowAccountsReceivableMenu: true,
        ShowPayLaterReports: true,
        ShowProductTypeSplit: true);

    public static TillLayout For(StoreFeaturesDto features) => new(
        ShowHardwareButton: features.MultipleProductTypesEnabled,
        ShowServiceButtons: features.ServiceProductsEnabled,
        ShowAccountsReceivableMenu: features.PayLaterEnabled,
        ShowPayLaterReports: features.PayLaterEnabled,
        ShowProductTypeSplit: features.MultipleProductTypesEnabled);
}
