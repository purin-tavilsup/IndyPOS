using System.Drawing;
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
/// impossible, the caption ink is identical (rows 108-122, 15px, with rows 123-127 clear below it)
/// and the welfare-card icon's visible ink is 174px wide in both a 195px and a 400px button. The
/// arithmetic that raised the alarm used declared asset sizes, which include transparent margin.
/// </para>
/// <para>
/// What is genuinely tight is that only ONE line of caption fits beneath the tallest bundled icon,
/// and the catalogue is data-driven — a future campaign can arrive with a longer name. These tests
/// pin the two margins that keep it readable. Everything they measure is read from shipped code:
/// the button from the real factory, the captions from <see cref="PaymentMethodSeeder"/>, and the
/// icon height from the real <c>PaymentMethodIcons</c> table — so bundling a taller icon or adding
/// a longer campaign name fails here rather than reaching a till.
/// </para>
/// </summary>
public class PaymentMethodButtonCaptionTests
{
    /// <summary>
    /// Widest caption that renders unclipped, established empirically rather than derived: a
    /// button was rendered at 195px and again at 400px and the caption's ink pixels compared,
    /// which puts the onset of real clipping between 184px and 189px depending on the glyphs.
    /// 184 is the strict end of that band, so this errs toward a false alarm rather than a miss.
    /// <para>
    /// Do NOT re-derive this as "195 minus border and inset". `MeasureText(text, font)` adds a
    /// fixed 10px of glyph-overhang padding that is not chrome, so that arithmetic mixes units
    /// and lands ~3px too lenient.
    /// </para>
    /// </summary>
    private const int MaxCaptionWidth = 184;

    /// <summary>The same allowance vertically, above and below the image-plus-text stack.</summary>
    private const int VerticalChrome = 8;

    public static TheoryData<string, string> SeededMethods()
    {
        var data = new TheoryData<string, string>();

        foreach (var (code, caption) in ShippedMethods())
            data.Add(code, caption);

        return data;
    }

    [Theory]
    [MemberData(nameof(SeededMethods))]
    public void CreatePaymentMethodButton_ForEverySeededCaption_ShouldRenderOnOneLine(
        string code, string caption)
    {
        using var button = CreateButton(code, caption);

        var width = TextRenderer.MeasureText(caption, button.Font).Width;

        width.Should()
             .BeLessThanOrEqualTo(MaxCaptionWidth,
                 $"'{caption}' has to fit on one line — with an icon above it, a caption this " +
                 "wide is hard-clipped mid-glyph rather than wrapped");
    }

    [Fact]
    public void CreatePaymentMethodButton_BeneathTheTallestBundledIcon_ShouldLeaveRoomForOneLine()
    {
        var tallestIcon = ShippedIconHeights().Max();

        using var button = CreateButton(TallestSeededMethod().Code, TallestSeededMethod().Caption);

        var roomForCaption = button.Height - tallestIcon - VerticalChrome;
        var lineHeight = TextRenderer.MeasureText(TallestSeededMethod().Caption, button.Font).Height;

        roomForCaption.Should()
                      .BeGreaterThanOrEqualTo(lineHeight,
                          $"the caption is drawn below the icon, and the tallest bundled icon is " +
                          $"{tallestIcon}px — shrinking the button or bundling a taller icon clips it");
    }

    /// <summary>
    /// The layout the margin tests assume. Without an icon actually attached and stacked above the
    /// text, both of them would be arithmetic on constants that no longer describe the button.
    /// </summary>
    [Fact]
    public void CreatePaymentMethodButton_ForASeededMethod_ShouldStackARealIconAboveTheCaption()
    {
        var seeded = TallestSeededMethod();

        using var button = CreateButton(seeded.Code, seeded.Caption);

        button.Image.Should()
              .NotBeNull($"'{seeded.Code}' is a seeded method and must resolve a bundled icon — " +
                         "without one these tests measure a layout the till does not use");

        button.TextImageRelation.Should().Be(TextImageRelation.ImageAboveText);
        button.Image!.Height.Should().Be(ShippedIconHeights().Max());
    }

    private static Button CreateButton(string code, string caption)
    {
        var factory = typeof(global::IndyPOS.Windows.Forms.UI.MainForm).Assembly
                          .GetType("IndyPOS.Windows.Forms.UI.Payment.AcceptPaymentForm")!
                          .GetMethod("CreatePaymentMethodButton", BindingFlags.NonPublic | BindingFlags.Static)
                      ?? throw new InvalidOperationException(
                          "AcceptPaymentForm.CreatePaymentMethodButton is gone — this guard is measuring nothing.");

        var method = new PaymentMethodDto(code, caption, PaymentMethodKind.Standard, true, 1);

        // 1.0f = 100% display scaling; PaymentMethodButtonScalingTests covers the rest.
        return (Button)factory.Invoke(null, [method, 0, 1.0f])!;
    }

    /// <summary>The seeded method carrying the tallest bundled icon — the worst case for height.</summary>
    private static (string Code, string Caption) TallestSeededMethod()
    {
        var icons = ShippedIcons();

        var withIcons = ShippedMethods()
                        .Where(method => icons.ContainsKey(method.Code))
                        .OrderByDescending(method => icons[method.Code].Height)
                        .ToList();

        if (withIcons.Count == 0)
            throw new InvalidOperationException(
                "No seeded payment method resolves a bundled icon — this guard is measuring nothing.");

        return withIcons[0];
    }

    private static IEnumerable<int> ShippedIconHeights() =>
        ShippedIcons().Values.Select(image => image.Height);

    /// <summary>
    /// The real icon table, so a taller asset is caught here rather than copied into a constant
    /// that silently goes stale.
    /// </summary>
    private static Dictionary<string, Image> ShippedIcons()
    {
        var field = typeof(global::IndyPOS.Windows.Forms.UI.MainForm).Assembly
                        .GetType("IndyPOS.Windows.Forms.UI.Payment.PaymentMethodIcons")!
                        .GetField("IconsByCode", BindingFlags.NonPublic | BindingFlags.Static)
                    ?? throw new InvalidOperationException(
                        "PaymentMethodIcons.IconsByCode is gone — this guard is measuring nothing.");

        return (Dictionary<string, Image>)field.GetValue(null)!;
    }

    /// <summary>
    /// The methods a store actually gets, read from the seeder rather than copied, so a new
    /// campaign is covered by this guard the moment it is added.
    /// </summary>
    private static IEnumerable<(string Code, string Caption)> ShippedMethods()
    {
        var defaults = typeof(PaymentMethodSeeder)
                           .GetField("Defaults", BindingFlags.NonPublic | BindingFlags.Static)
                           ?.GetValue(null) as Array
                       ?? throw new InvalidOperationException(
                           "PaymentMethodSeeder.Defaults is gone — this guard is measuring nothing.");

        foreach (var seed in defaults)
        {
            var type = seed.GetType();

            yield return (
                (string)type.GetProperty("Code")!.GetValue(seed)!,
                (string)type.GetProperty("DisplayName")!.GetValue(seed)!);
        }
    }
}
