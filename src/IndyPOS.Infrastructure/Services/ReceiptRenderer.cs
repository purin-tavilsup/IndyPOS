using System.Runtime.Versioning;
using IndyPOS.Application.Common.Receipts;

namespace IndyPOS.Infrastructure.Services;

/// <summary>
/// Draws receipt rows on a 58 mm roll. Only positions and fonts live here; every word comes from
/// <see cref="ReceiptLayout"/>. Spacing matches the v3 printer's.
/// </summary>
[SupportedOSPlatform("windows")]
public static class ReceiptRenderer
{
    private const int TopOfPage = 5;
    private const int RowHeight = 11;
    private const int StoreNameHeight = 30;
    private const int BannerHeight = 24;
    private const int StoreNameX = 60;
    private const int IndentX = 10;
    private const int PriceColumn = 135;

    private const string FontFamilyName = "FC Subject [Non-commercial] Reg";
    private const string RuleText = "-------------------------------------------------------";

    private static readonly Font BannerFont = new(FontFamilyName, 13, FontStyle.Bold);
    private static readonly Font StoreNameFont = new(FontFamilyName, 15);
    private static readonly Font TextFont = new(FontFamilyName, 7);
    private static readonly Font RuleFont = new(FontFamilyName, 5);

    /// <returns>The y position below the last row.</returns>
    public static int Draw(Graphics graphics, IReadOnlyList<ReceiptRow> rows)
    {
        var y = TopOfPage;

        foreach (var row in rows)
            y = DrawRow(graphics, row, y);

        return y;
    }

    private static int DrawRow(Graphics graphics, ReceiptRow row, int y)
    {
        switch (row.Style)
        {
            case ReceiptRowStyle.Banner:
                graphics.DrawString(row.Text, BannerFont, Brushes.Black, 0, y);
                return y + BannerHeight;
            case ReceiptRowStyle.StoreName:
                graphics.DrawString(row.Text, StoreNameFont, Brushes.Black, StoreNameX, y);
                return y + StoreNameHeight;
            case ReceiptRowStyle.Rule:
                graphics.DrawString(RuleText, RuleFont, Brushes.Black, 0, y);
                return y + RowHeight;
            default:
                DrawText(graphics, row, y);
                return y + RowHeight;
        }
    }

    private static void DrawText(Graphics graphics, ReceiptRow row, int y)
    {
        var x = row.Style == ReceiptRowStyle.Indented ? IndentX : 0;
        graphics.DrawString(row.Text, TextFont, Brushes.Black, x, y);

        if (row.Amount is not null)
            graphics.DrawString(row.Amount, TextFont, Brushes.Black, PriceColumn, y);
    }
}
