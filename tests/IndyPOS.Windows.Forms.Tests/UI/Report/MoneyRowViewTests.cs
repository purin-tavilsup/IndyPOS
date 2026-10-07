using System.Windows.Forms;
using FluentAssertions;
using IndyPOS.Application.Common.Models;
using IndyPOS.Windows.Forms.UI.Report;
using Xunit;

namespace IndyPOS.Windows.Forms.Tests.UI.Report;

public class MoneyRowViewTests
{
    // Controls.Clear() alone keeps every old row's window handles alive, and the overview refreshes all
    // day: a till that runs for days would end with "Error creating window handle".
    [Fact]
    public void Replace_WithRowsAlreadyShown_DisposesTheOldRows()
    {
        using var panel = new FlowLayoutPanel();
        MoneyRowView.Replace(panel, [new MoneyRow("เงินสด", 100m)]);
        var oldRow = panel.Controls[0];

        MoneyRowView.Replace(panel, [new MoneyRow("เงินสด", 120m)]);

        oldRow.IsDisposed.Should()
                         .BeTrue();
    }

    [Fact]
    public void Replace_WithTwoRows_ShowsOneControlPerRow()
    {
        using var panel = new FlowLayoutPanel();

        MoneyRowView.Replace(panel, [new MoneyRow("เงินสด", 100m), new MoneyRow("เงินโอน", 50m)]);

        panel.Controls.Count.Should()
                            .Be(2);
    }
}
