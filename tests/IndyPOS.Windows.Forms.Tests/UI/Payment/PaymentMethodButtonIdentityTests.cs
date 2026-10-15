using System.Reflection;
using System.Windows.Forms;
using FluentAssertions;
using IndyPOS.Application.UseCases.StoreHub.PaymentMethods;
using IndyPOS.Domain.Enums;
using IndyPOS.Infrastructure.Persistence.StoreHub.Seeders;
using Xunit;

namespace IndyPOS.Windows.Forms.Tests.UI.Payment;

/// <summary>
/// The payment-method buttons are built from the data-driven catalogue, so they carry no designer
/// <c>Name</c>. UI Automation then has only the Thai caption to match on, which is brittle for a test
/// driver and for the coming Avalonia port. These pin a stable identity:
/// <c>Control.Name = "PaymentMethod_{code}"</c>, which WinForms surfaces as the UIA AutomationId.
/// The captions keep their own guard in <see cref="PaymentMethodButtonCaptionTests"/>.
/// </summary>
public class PaymentMethodButtonIdentityTests
{
    private const string NamePrefix = "PaymentMethod_";

    public static TheoryData<string> SeededCodes()
    {
        var data = new TheoryData<string>();

        foreach (var code in ShippedCodes())
            data.Add(code);

        return data;
    }

    [Theory]
    [MemberData(nameof(SeededCodes))]
    public void CreatePaymentMethodButton_ForEverySeededMethod_NamesButtonByCode(string code)
    {
        using var button = CreateButton(code);

        button.Name.Should().Be(NamePrefix + code);
    }

    [Fact]
    public void CreatePaymentMethodButton_WithACodeOutsideTheCatalogue_StillNamesButtonByThatCode()
    {
        using var button = CreateButton("SomeFutureCampaign");

        button.Name.Should().Be(NamePrefix + "SomeFutureCampaign");
    }

    [Fact]
    public void CreatePaymentMethodButton_ForTwoDifferentMethods_GivesDistinctNames()
    {
        using var cash = CreateButton("Cash");
        using var transfer = CreateButton("MoneyTransfer");

        cash.Name.Should().NotBe(transfer.Name);
    }

    private static Button CreateButton(string code)
    {
        var factory = typeof(global::IndyPOS.Windows.Forms.UI.MainForm).Assembly
                          .GetType("IndyPOS.Windows.Forms.UI.Payment.AcceptPaymentForm")!
                          .GetMethod("CreatePaymentMethodButton", BindingFlags.NonPublic | BindingFlags.Static)
                      ?? throw new InvalidOperationException(
                          "AcceptPaymentForm.CreatePaymentMethodButton is gone — this guard is measuring nothing.");

        var method = new PaymentMethodDto(code, code, PaymentMethodKind.Standard, true, 1);

        // 1.0f = 100% display scaling; index 0. Neither affects the button's Name.
        return (Button)factory.Invoke(null, [method, 0, 1.0f])!;
    }

    /// <summary>
    /// The codes a store actually gets, read from the seeder rather than copied, so a new campaign is
    /// covered by this guard the moment it is added.
    /// </summary>
    private static IEnumerable<string> ShippedCodes()
    {
        var defaults = typeof(PaymentMethodSeeder)
                           .GetField("Defaults", BindingFlags.NonPublic | BindingFlags.Static)
                           ?.GetValue(null) as Array
                       ?? throw new InvalidOperationException(
                           "PaymentMethodSeeder.Defaults is gone — this guard is measuring nothing.");

        foreach (var seed in defaults)
            yield return (string)seed.GetType().GetProperty("Code")!.GetValue(seed)!;
    }
}
