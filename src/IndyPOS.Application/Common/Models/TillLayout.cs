namespace IndyPOS.Application.Common.Models;

/// <summary>
/// Which of the till's optional pieces this store has, decided from its store-type features alone.
/// The forms only apply it (WinForms today, Avalonia later).
/// </summary>
public sealed record TillLayout(bool ShowHardwareButton, bool ShowServiceButtons, bool ShowAccountsReceivableMenu)
{
    public static TillLayout For(StoreFeaturesDto features) => new(
        ShowHardwareButton: features.MultipleProductTypesEnabled,
        ShowServiceButtons: features.ServiceProductsEnabled,
        ShowAccountsReceivableMenu: features.PayLaterEnabled);
}
