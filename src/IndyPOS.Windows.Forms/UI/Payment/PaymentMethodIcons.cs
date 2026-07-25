using IndyPOS.Application.Common.Constants;
using System.Diagnostics.CodeAnalysis;

namespace IndyPOS.Windows.Forms.UI.Payment;

/// <summary>
/// Icons for the known payment-method codes, keyed by catalog Code.
/// <para>
/// The catalog is data-driven, so a brand-new government campaign can appear without a
/// matching asset — those buttons render text-only rather than blocking the campaign.
/// </para>
/// </summary>
[ExcludeFromCodeCoverage]
internal static class PaymentMethodIcons
{
    private static readonly Dictionary<string, Image> IconsByCode =
        new(StringComparer.OrdinalIgnoreCase)
        {
            [PaymentMethodCodes.Cash] = Properties.Resources.Money_80,
            [PaymentMethodCodes.MoneyTransfer] = Properties.Resources.Payment_MoneyTransfer_100,
            [PaymentMethodCodes.PayLater] = Properties.Resources.Customer_Acounts_50,
            [PaymentMethodCodes.WelfareCard] = Properties.Resources.Payment_PracharatCard_100,
            [PaymentMethodCodes.M33WeLove] = Properties.Resources.Payment_WeLove_100,
            [PaymentMethodCodes.FiftyFifty] = Properties.Resources.Payment_KLK_100,
            [PaymentMethodCodes.WeWin] = Properties.Resources.Payment_WeWin_100
        };

    /// <summary>Returns the icon for <paramref name="code"/>, or null when none is bundled.</summary>
    public static Image? For(string code) =>
        IconsByCode.GetValueOrDefault(code);
}
