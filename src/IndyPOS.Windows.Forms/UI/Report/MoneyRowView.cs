using IndyPOS.Application.Common.Models;

namespace IndyPOS.Windows.Forms.UI.Report;

/// <summary>Draws one money row as the designer drew the fixed ones: caption left, amount right.</summary>
public static class MoneyRowView
{
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
        foreach (var row in rows)
            panel.Controls.Add(Create(row));
        panel.ResumeLayout();
    }

    public static Control Create(MoneyRow row)
    {
        var panel = new Panel { BackColor = RowBack, Size = new Size(406, 45), Margin = new Padding(0, 0, 6, 6) };
        panel.Controls.Add(new Label
        {
            BackColor = LabelBack, Dock = DockStyle.Fill, ForeColor = Color.Gainsboro,
            Font = new Font(FontName, 15.75F, FontStyle.Regular, GraphicsUnit.Point),
            Text = $"{row.Amount:N2}", TextAlign = ContentAlignment.MiddleRight
        });
        panel.Controls.Add(new Label
        {
            BackColor = LabelBack, Dock = DockStyle.Left, Width = 196, ForeColor = Color.Gainsboro,
            Font = new Font(FontName, 12F, FontStyle.Regular, GraphicsUnit.Point),
            Text = row.Title, TextAlign = ContentAlignment.MiddleCenter
        });
        return panel;
    }
}
