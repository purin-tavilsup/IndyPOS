using IndyPOS.Application.Common.Models;

namespace IndyPOS.Windows.Forms.UI.Report;

/// <summary>Draws one money row as the designer drew the fixed ones: caption left, amount right.</summary>
public static class MoneyRowView
{
    public const int RowsPerLine = 3;

    // The designer's row: 406 x 45, the caption 196 of it, 6 px apart. Rows keep these proportions.
    private const float DesignWidth = 406f;
    private const float DesignHeight = 45f;
    private const float DesignCaptionWidth = 196f;
    private const int Gap = 6;

    private const string FontName = "FC Subject [Non-commercial] Reg";
    private static readonly Color RowBack = Color.FromArgb(34, 34, 34);
    private static readonly Color LabelBack = Color.FromArgb(35, 35, 35);

    public static void Replace(FlowLayoutPanel panel, IReadOnlyList<MoneyRow> rows)
    {
        panel.SuspendLayout();
        // Dispose, not just Clear: a cleared control keeps its window handles, and the overview refreshes
        // all day. Disposing a child also removes it from the panel.
        foreach (var old in panel.Controls.Cast<Control>().ToList())
            old.Dispose();

        var width = RowWidth(panel);
        foreach (var row in rows)
            panel.Controls.Add(Create(row, width));
        panel.ResumeLayout();
    }

    // A third of the panel, less room for a scrollbar: the panel scales with the display but these rows are
    // built in code, so a fixed width would wrap to two a line once a scrollbar appears.
    private static int RowWidth(Control panel) =>
        (panel.ClientSize.Width - SystemInformation.VerticalScrollBarWidth) / RowsPerLine - Gap;

    private static Control Create(MoneyRow row, int width)
    {
        var scale = width / DesignWidth;
        var panel = new Panel
        {
            BackColor = RowBack,
            Size = new Size(width, (int)Math.Round(DesignHeight * scale)),
            Margin = new Padding(0, 0, Gap, Gap)
        };
        panel.Controls.Add(new Label
        {
            BackColor = LabelBack, Dock = DockStyle.Fill, ForeColor = Color.Gainsboro,
            Font = new Font(FontName, 15.75F, FontStyle.Regular, GraphicsUnit.Point),
            Text = $"{row.Amount:N2}", TextAlign = ContentAlignment.MiddleRight
        });
        panel.Controls.Add(new Label
        {
            BackColor = LabelBack, Dock = DockStyle.Left, Width = (int)Math.Round(DesignCaptionWidth * scale),
            ForeColor = Color.Gainsboro,
            Font = new Font(FontName, 12F, FontStyle.Regular, GraphicsUnit.Point),
            Text = row.Title, TextAlign = ContentAlignment.MiddleCenter
        });
        return panel;
    }
}
