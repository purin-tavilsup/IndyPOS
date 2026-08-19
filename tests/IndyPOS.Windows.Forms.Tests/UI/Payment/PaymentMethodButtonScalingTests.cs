using System.Drawing;
using System.Reflection;
using System.Windows.Forms;
using FluentAssertions;
using IndyPOS.Application.UseCases.StoreHub.PaymentMethods;
using IndyPOS.Domain.Enums;
using Xunit;

namespace IndyPOS.Windows.Forms.Tests.UI.Payment;

/// <summary>
/// The payment buttons are built in code with pixel literals, from the async load path — so after
/// <c>PerformAutoScale</c> has already run. The 12pt caption font is point-sized and therefore
/// renders larger as display scaling rises, while the button did not, so from about 145% the
/// caption wrapped to two lines and was painted on top of the card artwork.
/// <para>
/// Scaling is passed in rather than read from the environment, so these run identically on any
/// machine. The equivalence is exact: 12pt at 144dpi and 18pt at 96dpi produce the same
/// <c>LOGFONT.lfHeight</c>, so raising the point size stands in for raising the scale factor.
/// </para>
/// </summary>
public class PaymentMethodButtonScalingTests
{
    /// <summary>Matches VerticalChrome in <see cref="PaymentMethodButtonCaptionTests"/>.</summary>
    private const int VerticalChrome = 8;

    private const float DesignFontPointSize = 12F;

    [Theory]
    [InlineData(1.00f)]
    [InlineData(1.25f)]
    [InlineData(1.50f)]
    [InlineData(1.75f)]
    [InlineData(2.00f)]
    public void CreatePaymentMethodButton_AtEveryDisplayScaling_ShouldLeaveRoomForTheCaption(float scale)
    {
        using var button = CreateButton(scale);
        using var scaledFont = ScaledFont(scale);

        var roomForCaption = button.Height - TallestIconHeight() - VerticalChrome;
        var lineHeight = TextRenderer.MeasureText(Caption, scaledFont).Height;

        roomForCaption.Should()
                      .BeGreaterThanOrEqualTo(lineHeight,
                          $"at {scale:P0} scaling the caption renders {lineHeight}px tall, and a " +
                          "button that does not scale with it paints the caption over the icon");
    }

    [Theory]
    [InlineData(1.00f)]
    [InlineData(1.50f)]
    [InlineData(2.00f)]
    public void CreatePaymentMethodButton_AtEveryDisplayScaling_ShouldKeepTheCaptionOnOneLine(float scale)
    {
        using var button = CreateButton(scale);
        using var scaledFont = ScaledFont(scale);

        var width = TextRenderer.MeasureText(Caption, scaledFont).Width;

        width.Should()
             .BeLessThanOrEqualTo(button.Width - VerticalChrome,
                 $"at {scale:P0} scaling the caption is {width}px wide; a button that does not " +
                 "scale with it wraps the caption onto a second line it has no room for");
    }

    [Fact]
    public void CreatePaymentMethodButton_AtDesignScaling_ShouldKeepTheShippedGeometry()
    {
        using var button = CreateButton(1.0f);

        button.Size.Should().Be(new Size(195, 129), "scaling must be a no-op at 100%");
        button.Location.Should().Be(new Point(10, 16));
    }

    [Fact]
    public void CreatePaymentMethodButton_WhenScaled_ShouldScaleTheGridSpacingToo()
    {
        using var first = CreateButton(2.0f, index: 0);
        using var second = CreateButton(2.0f, index: 1);
        using var third = CreateButton(2.0f, index: 2);

        (second.Location.X - first.Location.X).Should()
            .BeGreaterThanOrEqualTo(first.Width, "columns must not overlap once the buttons grow");

        (third.Location.Y - first.Location.Y).Should()
            .BeGreaterThanOrEqualTo(first.Height, "rows must not overlap once the buttons grow");
    }

    private const string Caption = "บัตรสวัสดิการแห่งรัฐ";

    private static Font ScaledFont(float scale) =>
        new("Leelawadee UI", DesignFontPointSize * scale, FontStyle.Regular, GraphicsUnit.Point);

    private static int TallestIconHeight()
    {
        var field = typeof(global::IndyPOS.Windows.Forms.UI.MainForm).Assembly
                        .GetType("IndyPOS.Windows.Forms.UI.Payment.PaymentMethodIcons")!
                        .GetField("IconsByCode", BindingFlags.NonPublic | BindingFlags.Static)!;

        return ((Dictionary<string, Image>)field.GetValue(null)!).Values.Max(image => image.Height);
    }

    private static Button CreateButton(float scale, int index = 0)
    {
        var factory = typeof(global::IndyPOS.Windows.Forms.UI.MainForm).Assembly
                          .GetType("IndyPOS.Windows.Forms.UI.Payment.AcceptPaymentForm")!
                          .GetMethod("CreatePaymentMethodButton", BindingFlags.NonPublic | BindingFlags.Static)
                      ?? throw new InvalidOperationException(
                          "AcceptPaymentForm.CreatePaymentMethodButton is gone — this guard is measuring nothing.");

        var method = new PaymentMethodDto("WelfareCard", Caption, PaymentMethodKind.GovernmentCampaign, true, 3);

        return (Button)factory.Invoke(null, [method, index, scale])!;
    }
}
