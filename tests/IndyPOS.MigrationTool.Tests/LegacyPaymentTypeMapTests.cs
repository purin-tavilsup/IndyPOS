using FluentAssertions;
using IndyPOS.Application.Common.Constants;
using IndyPOS.MigrationTool.Services;

namespace IndyPOS.MigrationTool.Tests;

/// <summary>
/// The legacy PaymentTypeId -> catalogue Code mapping, verified against the ids and Thai labels
/// in the real store database (.planning/indypos-overhaul/sqlite_database/Store.db).
/// <para>The previous mapping was shifted: id 2 (ลงบัญชี / PayLater) was written as "Card",
/// id 3 (บัตรสวัสดิการแห่งรัฐ / WelfareCard) as "Transfer", id 5 (โอนเข้าบัญชี / MoneyTransfer) as
/// "WelfareCard", and the campaigns as "Other" - roughly 15% of ฿21.2M attributed to the wrong
/// method, with ฿836k of customer credit invisible to PayLater tracking.</para>
/// </summary>
public class LegacyPaymentTypeMapTests
{
    [Theory]
    [InlineData(1, PaymentMethodCodes.Cash)]          // เงินสด
    [InlineData(2, PaymentMethodCodes.PayLater)]      // ลงบัญชี
    [InlineData(3, PaymentMethodCodes.WelfareCard)]   // บัตรสวัสดิการแห่งรัฐ
    [InlineData(4, PaymentMethodCodes.M33WeLove)]     // ม.33
    [InlineData(5, PaymentMethodCodes.MoneyTransfer)] // โอนเข้าบัญชี
    [InlineData(7, PaymentMethodCodes.FiftyFifty)]    // คนละครึ่ง
    [InlineData(8, PaymentMethodCodes.WeWin)]         // เราชนะ
    public void ToCode_ForEachLegacyPaymentType_ShouldReturnTheCatalogueCode(int legacyId, string expected)
    {
        LegacyPaymentTypeMap.ToCode(legacyId).Should().Be(expected);
    }

    [Fact]
    public void ToCode_ForInstallments_ShouldReturnNull()
    {
        // id 6 (ผ่อนชำระ) has no catalogue equivalent: instalments are their own table, and the
        // real store data has zero Payment rows for it.
        LegacyPaymentTypeMap.ToCode(6).Should().BeNull();
    }

    [Fact]
    public void ToCode_ForAnUnknownId_ShouldReturnNullRatherThanGuess()
    {
        // The old code funnelled anything unrecognised into "Other", which is not a catalogue
        // code - money quietly attributed to a method that does not exist.
        LegacyPaymentTypeMap.ToCode(99).Should().BeNull();
    }

    [Fact]
    public void EveryMappedCode_ShouldExistInTheCatalogue()
    {
        // Guards the drift that caused this: a code the catalogue does not define cannot be
        // resolved by reports, the payment buttons, or the refund gate.
        var catalogue = new[]
        {
            PaymentMethodCodes.Cash, PaymentMethodCodes.MoneyTransfer, PaymentMethodCodes.WelfareCard,
            PaymentMethodCodes.PayLater, PaymentMethodCodes.M33WeLove, PaymentMethodCodes.FiftyFifty,
            PaymentMethodCodes.WeWin
        };

        LegacyPaymentTypeMap.All.Values.Should().OnlyContain(code => catalogue.Contains(code));
    }

    [Fact]
    public void All_ShouldNotMapTwoLegacyIdsToTheSameCode()
    {
        // A shifted mapping shows up here too: collisions mean one method absorbed another's money.
        LegacyPaymentTypeMap.All.Values.Should().OnlyHaveUniqueItems();
    }
}
