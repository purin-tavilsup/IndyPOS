using IndyPOS.Windows.Forms.UI.ModernUI;

namespace IndyPOS.Windows.Forms.UI.Report;

/// <summary>The till's dark look, as the designer-built screens have it, for views built in code.</summary>
public static class TillStyle
{
    public const string FontName = "FC Subject [Non-commercial] Reg";
    public static readonly Color Background = Color.FromArgb(38, 38, 38);
    public static readonly Color Accent = Color.FromArgb(50, 190, 166);

    public static Label Label(string name, float size = 12F) => new()
    {
        Name = name,
        AutoSize = false,
        Height = (int)(size * 2.2F),
        ForeColor = Color.Gainsboro,
        Font = new Font(FontName, size),
        TextAlign = ContentAlignment.MiddleLeft
    };

    public static ModernButton Button(string name, string text, Size size) => new()
    {
        Name = name,
        Text = text,
        Size = size,
        BackColor = Background,
        BackgroundColor = Background,
        BorderColor = Accent,
        BorderRadius = 5,
        BorderSize = 1,
        FlatStyle = FlatStyle.Flat,
        Font = new Font(FontName, 14F),
        ForeColor = Color.White,
        TextColor = Color.White
    };
}
