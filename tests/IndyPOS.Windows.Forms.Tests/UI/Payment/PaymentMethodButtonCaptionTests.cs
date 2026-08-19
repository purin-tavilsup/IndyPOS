using System.Reflection;
using System.Windows.Forms;
using FluentAssertions;
using IndyPOS.Application.UseCases.StoreHub.PaymentMethods;
using IndyPOS.Domain.Enums;
using IndyPOS.Infrastructure.Persistence.StoreHub.Seeders;
using Xunit;

namespace IndyPOS.Windows.Forms.Tests.UI.Payment;

/// <summary>
/// Guards the payment buttons against a caption that no longer fits.
/// <para>
/// PLAN.md carried an owed visual smoke for months, suspecting <c>บัตรสวัสดิการแห่งรัฐ</c> was
/// clipped on the 195x129 button. It is not: rendered against a 195x260 button, where clipping is
/// impossible, the caption ink is identical (11px, 5px of gap below it) and the welfare-card icon's
/// visible ink is 174px wide in both a 195px and a 400px button. The arithmetic that raised the
/// alarm used declared asset sizes, which include transparent margin.
/// </para>
/// <para>
/// What is genuinely tight is that only ONE line of caption fits beneath a 100px icon, and the
/// catalogue is data-driven — a future campaign can arrive with a longer name. These tests pin the
/// two margins that keep it readable, driven by the real factory and the real seeded catalogue so
/// neither can drift from what a till renders.
/// </para>
/// </summary>
public class PaymentMethodButtonCaptionTests
{
    /// <summary>1px flat border plus the 3px text inset ButtonBase applies, on both sides.</summary>
    private const int HorizontalChrome = 8;

    /// <summary>The same allowance vertically, above and below the image-plus-text stack.</summary>
    private const int VerticalChrome = 8;

    /// <summary>The tallest icon bundled for a payment method (see PaymentMethodIcons).</summary>
    private const int TallestIconHeight = 100;

    public static TheoryData<string> SeededCaptions()
    {
        var data = new TheoryData<string>();

        foreach (var caption in ShippedCaptions())
            data.Add(caption);

        return data;
    }

    [Theory]
    [MemberData(nameof(SeededCaptions))]
    public void CreatePaymentMethodButton_ForEverySeededCaption_ShouldRenderOnOneLine(string caption)
    {
        using var button = CreateButton(caption);

        var width = TextRenderer.MeasureText(caption, button.Font).Width;

        width.Should()
             .BeLessThanOrEqualTo(button.Width - HorizontalChrome,
                 $"'{caption}' has to fit on one line — a second line does not fit under the icon");
    }

    [Fact]
    public void CreatePaymentMethodButton_BeneathTheTallestIcon_ShouldLeaveRoomForOneLine()
    {
        using var button = CreateButton("บัตรสวัสดิการแห่งรัฐ");

        var roomForCaption = button.Height - TallestIconHeight - VerticalChrome;
        var lineHeight = TextRenderer.MeasureText("บัตรสวัสดิการแห่งรัฐ", button.Font).Height;

        roomForCaption.Should()
                      .BeGreaterThanOrEqualTo(lineHeight,
                          "the caption is drawn below the icon, so shrinking the button or " +
                          "bundling a taller icon would clip it");
    }

    private static Button CreateButton(string caption)
    {
        var factory = typeof(global::IndyPOS.Windows.Forms.UI.MainForm).Assembly
                          .GetType("IndyPOS.Windows.Forms.UI.Payment.AcceptPaymentForm")!
                          .GetMethod("CreatePaymentMethodButton", BindingFlags.NonPublic | BindingFlags.Static)
                      ?? throw new InvalidOperationException(
                          "AcceptPaymentForm.CreatePaymentMethodButton is gone — this guard is measuring nothing.");

        var method = new PaymentMethodDto("Code", caption, PaymentMethodKind.Standard, true, 1);

        return (Button)factory.Invoke(null, [method, 0])!;
    }

    /// <summary>
    /// The captions a store actually gets, read from the seeder rather than copied, so a new
    /// campaign is covered by this guard the moment it is added.
    /// </summary>
    private static IEnumerable<string> ShippedCaptions()
    {
        var defaults = typeof(PaymentMethodSeeder)
                           .GetField("Defaults", BindingFlags.NonPublic | BindingFlags.Static)
                           ?.GetValue(null) as Array
                       ?? throw new InvalidOperationException(
                           "PaymentMethodSeeder.Defaults is gone — this guard is measuring nothing.");

        foreach (var seed in defaults)
            yield return (string)seed.GetType().GetProperty("DisplayName")!.GetValue(seed)!;
    }
}
