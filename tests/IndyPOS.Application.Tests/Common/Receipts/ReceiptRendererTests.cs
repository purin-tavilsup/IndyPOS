using System.Drawing;
using System.Globalization;
using FluentAssertions;
using IndyPOS.Application.Common.Receipts;
using IndyPOS.Infrastructure.Services;
using Xunit;

namespace IndyPOS.Application.Tests.Common.Receipts;

public class ReceiptRendererTests
{
    private const int TopOfPage = 5;

    [Fact]
    public void Draw_WithNoRows_LeavesThePositionAtTheTop()
    {
        using var bitmap = new Bitmap(200, 100);
        using var graphics = Graphics.FromImage(bitmap);

        var bottom = ReceiptRenderer.Draw(graphics, []);

        bottom.Should()
              .Be(TopOfPage);
    }

    [Fact]
    public void Draw_WithACopyReceipt_MovesDownForEveryRow()
    {
        var rows = ReceiptLayout.Rows(new ReceiptHeader("Store", "a", "b", "c"), CopyReceipt(), CultureInfo.InvariantCulture);
        using var bitmap = new Bitmap(200, 1000);
        using var graphics = Graphics.FromImage(bitmap);

        var bottom = ReceiptRenderer.Draw(graphics, rows);

        bottom.Should()
              .BeGreaterThan(TopOfPage + rows.Count * 5);
    }

    private static ReceiptDocument CopyReceipt() => new(
        42, new DateTime(2026, 10, 15, 14, 30, 0), "Malee Cashier",
        [new ReceiptItem("Nails 1kg", null, 2, 50m, 100m)],
        [new ReceiptPayment("เงินสด", 500m, null)],
        100m, 500m, 400m, false,
        new ReceiptReprint(new DateTime(2026, 10, 16, 9, 5, 0), "Somchai Manager"));
}
