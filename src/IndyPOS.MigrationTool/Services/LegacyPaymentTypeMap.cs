using IndyPOS.Application.Common.Constants;

namespace IndyPOS.MigrationTool.Services;

/// <summary>
/// Legacy SQLite <c>PaymentType.PaymentTypeId</c> to v4 catalogue <c>Code</c>.
/// <para>Taken from the real store database's own <c>PaymentType</c> lookup table, not from
/// guesswork:</para>
/// <list type="table">
/// <item><term>1</term><description>เงินสด - Cash</description></item>
/// <item><term>2</term><description>ลงบัญชี - PayLater (charged to the customer's account)</description></item>
/// <item><term>3</term><description>บัตรสวัสดิการแห่งรัฐ - state WelfareCard</description></item>
/// <item><term>4</term><description>ม.33 - the M33WeLove campaign</description></item>
/// <item><term>5</term><description>โอนเข้าบัญชี - MoneyTransfer. The legacy label says "transfer
/// into account", but per Pond (2026-07-29) this is the store's CASHLESS bucket: debit tap,
/// Apple Pay, Google Pay. It is a Standard method, NOT a government campaign - which is what
/// the v4 catalogue already says, so no reclassification is needed.</description></item>
/// <item><term>6</term><description>ผ่อนชำระ - instalments; NO catalogue equivalent (own table, zero Payment rows)</description></item>
/// <item><term>7</term><description>คนละครึ่ง - the FiftyFifty campaign</description></item>
/// <item><term>8</term><description>เราชนะ - the WeWin campaign</description></item>
/// </list>
/// <para>Shared by the migrator and the verifier deliberately: the verifier compares per-method
/// totals, and it can only do that honestly if it derives expectations from the same table the
/// migration wrote from.</para>
/// </summary>
public static class LegacyPaymentTypeMap
{
    public static readonly IReadOnlyDictionary<int, string> All = new Dictionary<int, string>
    {
        [1] = PaymentMethodCodes.Cash,
        [2] = PaymentMethodCodes.PayLater,
        [3] = PaymentMethodCodes.WelfareCard,
        [4] = PaymentMethodCodes.M33WeLove,
        [5] = PaymentMethodCodes.MoneyTransfer,
        [7] = PaymentMethodCodes.FiftyFifty,
        [8] = PaymentMethodCodes.WeWin
    };

    /// <returns>
    /// The catalogue code, or <c>null</c> when the id has no equivalent. Null rather than a
    /// fallback such as "Other": that is not a catalogue code, so it silently attributes money to
    /// a method nothing can resolve. Callers must refuse the row instead.
    /// </returns>
    public static string? ToCode(int legacyPaymentTypeId) =>
        All.TryGetValue(legacyPaymentTypeId, out var code) ? code : null;
}
