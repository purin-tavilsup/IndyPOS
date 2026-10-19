using System.Drawing;
using System.Windows.Forms;
using FluentAssertions;
using IndyPOS.Application.Common.Enums;
using IndyPOS.Application.UseCases.StoreHub.CashDrawer.Summary;
using IndyPOS.Windows.Forms.UI.CashDrawer;
using Moq;
using Xunit;
using static IndyPOS.Windows.Forms.Tests.UI.CashDrawer.CashDrawerFixture;

namespace IndyPOS.Windows.Forms.Tests.UI.CashDrawer;

/// <summary>
/// The panel at the size the till gives it (MainForm's ActivePanel, 1502 x 972 at 100%): every button, box and
/// money figure must be wholly inside its parents, and every figure wide enough for its text. Built in code,
/// so nothing but these tests (and a live screenshot) catches a clipped control.
/// </summary>
public class CashDrawerPanelLayoutTests
{
    private static readonly Size ActivePanel = new(1502, 972);
    private const decimal LargeExpectedCash = 123_456.75m;    // GeneralHardware routinely holds over ฿10,000
    private const decimal LargeShortfall = -12_345.50m;

    public static readonly TheoryData<string> Controls = new()
    {
        "AddPayoutButton", "EditPayoutButton", "DeletePayoutButton", "PayoutAmountBox", "PayoutTextBox", "PayoutCategoryBox",
        "AddFloatButton", "DeleteFloatButton", "AddDebtRepaymentButton", "DeleteDebtRepaymentButton",
        "BankNote1000CountBox", "Coin1CountBox", "TypedTotalLabel", "SaveCountButton", "ResetCountButton",
        "ExpectedCashLabel", "CountedCashLabel", "CashDifferenceLabel", "RefreshCashDrawerButton"
    };

    [Theory]
    [MemberData(nameof(Controls))]
    public async Task Open_AtTheTillsSize_KeepsTheControlInsideItsParents(string name)
    {
        using var form = await OpenedAsync();

        Outside(Find(form, name)).Should()
                                 .BeNull();
    }

    [Theory]
    [InlineData("ExpectedCashLabel")]
    [InlineData("CashDifferenceLabel")]
    public async Task Open_WithLargeAmounts_FitsTheFigure(string name)
    {
        using var form = await OpenedAsync();
        var label = Find(form, name);

        label.Width.Should()
                   .BeGreaterThanOrEqualTo(TextRenderer.MeasureText(label.Text, label.Font).Width, label.Text);
    }

    [Fact]
    public async Task Open_AtTheTillsSize_FitsEverySummaryCaption()
    {
        using var form = await OpenedAsync();

        var clipped = Find(form, "CashSummaryRows").Controls.Cast<Control>()
                                                   .SelectMany(row => row.Controls.Cast<Control>())
                                                   .Where(label => label.Width < TextRenderer.MeasureText(label.Text, label.Font).Width)
                                                   .Select(label => label.Text);

        clipped.Should()
               .BeEmpty();
    }

    // A GeneralHardware manager: every section, the category picker and the day picker all show.
    private static async Task<Form> OpenedAsync()
    {
        var client = EmptyDay(Today, expectedCash: LargeExpectedCash);
        client.Setup(c => c.GetCashSummaryAsync(Today, It.IsAny<CancellationToken>()))
              .ReturnsAsync(Summary(Today, true, LargeExpectedCash) with { CountedCash = LargeExpectedCash + LargeShortfall, CashDifference = LargeShortfall });
        var form = new Form { ClientSize = ActivePanel, AutoScaleMode = AutoScaleMode.None };
        var panel = new CashDrawerPanel(Factory(client.Object, LoggedIn(UserRole.StoreManager)), Features(GeneralHardware))
        {
            Dock = DockStyle.Fill
        };
        form.Controls.Add(panel);
        form.CreateControl();

        await panel.OpenAsync();
        form.PerformLayout();

        return form;
    }

    // The first ancestor that does not wholly contain the control, as "control in parent", or null.
    private static string? Outside(Control control)
    {
        var bounds = control.Bounds;
        for (var parent = control.Parent; parent is not null and not Form; parent = parent.Parent)
        {
            if (!parent.ClientRectangle.Contains(bounds))
                return $"{control.Name} {bounds} outside {parent.Name}/{parent.GetType().Name} {parent.ClientRectangle}";

            bounds.Offset(parent.Location);
        }

        return null;
    }
}
