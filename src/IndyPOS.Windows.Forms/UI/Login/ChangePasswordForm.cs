using System.Runtime.Versioning;
using IndyPOS.Windows.Forms.UI.ModernUI;

namespace IndyPOS.Windows.Forms.UI.Login;

/// <summary>
/// Modal shown on first login when the admin bootstrap password must be rotated.
/// Asks for a new password + confirmation only; the current (bootstrap) password
/// is supplied by the coordinator. Styled to match the app's dark theme
/// (see <see cref="MessageForm"/>).
/// </summary>
[SupportedOSPlatform("windows")]
public class ChangePasswordForm : Form
{
    private const int MinLength = 8;

    // Layout
    private const int LeftMargin = 40;
    private const int FieldWidth = 380;
    private const string ThemeFontName = "FC Subject [Non-commercial] Reg";

    // App dark-theme palette (mirrors MessageForm).
    private static readonly Color FormBack = Color.FromArgb(38, 38, 38);
    private static readonly Color PanelBack = Color.FromArgb(30, 30, 30);
    private static readonly Color LabelColor = Color.Gainsboro;
    private static readonly Color AccentTeal = Color.FromArgb(50, 190, 166);
    private static readonly Color AccentRed = Color.FromArgb(224, 79, 95);
    private static readonly Color FieldResting = Color.FromArgb(120, 120, 120);

    private readonly ModernTextBox _newPassword;
    private readonly ModernTextBox _confirmPassword;
    private readonly Label _error;

    /// <summary>The accepted new password, or null if the dialog was cancelled.</summary>
    public string? NewPassword { get; private set; }

    public ChangePasswordForm()
    {
        Text = "Set a New Password";
        FormBorderStyle = FormBorderStyle.None;
        StartPosition = FormStartPosition.CenterParent;
        MaximizeBox = false;
        MinimizeBox = false;
        BackColor = FormBack;
        ClientSize = new Size(LeftMargin + FieldWidth + LeftMargin, 380); // 460 x 380
        TopMost = true;
        Font = new Font(ThemeFontName, 12F);

        var panel = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = PanelBack,
            BorderStyle = BorderStyle.FixedSingle
        };

        var title = new Label
        {
            Text = "Set a New Password",
            Font = new Font(ThemeFontName, 15F, FontStyle.Bold),
            ForeColor = Color.White,
            AutoSize = true,
            Location = new Point(LeftMargin, 24)
        };

        var prompt = new Label
        {
            Text = "You must set a new password before continuing.",
            Font = new Font(ThemeFontName, 11F),
            ForeColor = LabelColor,
            AutoSize = false,
            Size = new Size(FieldWidth, 24),
            Location = new Point(LeftMargin, 60)
        };

        var newLabel = MakeFieldLabel("New password:", new Point(LeftMargin, 106));
        _newPassword = MakeField(new Point(LeftMargin, 132));
        _newPassword.PasswordChar = true;

        var confirmLabel = MakeFieldLabel("Confirm password:", new Point(LeftMargin, 188));
        _confirmPassword = MakeField(new Point(LeftMargin, 214));
        _confirmPassword.PasswordChar = true;

        // Teal underline on focus (matches the login field), reverting on blur.
        // Wired per-field so the shared ModernTextBox control is untouched.
        foreach (var field in new[] { _newPassword, _confirmPassword })
        {
            var f = field;
            f.Enter += (_, _) => f.BorderColor = AccentTeal;
            f.Leave += (_, _) => f.BorderColor = FieldResting;
        }

        _error = new Label
        {
            ForeColor = AccentRed,
            Font = new Font(ThemeFontName, 10F),
            AutoSize = false,
            Size = new Size(FieldWidth, 22),
            Location = new Point(LeftMargin, 262),
            TextAlign = ContentAlignment.MiddleLeft
        };

        // Right-aligned button pair on one baseline: primary rightmost, Cancel beside it.
        // Well below the error row so nothing overlaps.
        const int buttonY = 312;
        var rightEdge = LeftMargin + FieldWidth; // 420

        var okButton = new ModernButton
        {
            Text = "Set Password",
            Size = new Size(150, 44),
            Location = new Point(rightEdge - 150, buttonY),
            BackColor = FormBack,
            BackgroundColor = FormBack,
            ForeColor = Color.White,
            TextColor = Color.White,
            BorderColor = AccentTeal,
            BorderRadius = 20,
            BorderSize = 1,
            Font = new Font(ThemeFontName, 12F)
        };
        okButton.Click += OnOk;

        var cancelButton = new ModernButton
        {
            Text = "Cancel",
            Size = new Size(110, 44),
            Location = new Point(rightEdge - 150 - 12 - 110, buttonY),
            BackColor = FormBack,
            BackgroundColor = FormBack,
            ForeColor = Color.White,
            TextColor = Color.White,
            BorderColor = AccentRed,
            BorderRadius = 20,
            BorderSize = 1,
            Font = new Font(ThemeFontName, 12F),
            DialogResult = System.Windows.Forms.DialogResult.Cancel
        };

        panel.Controls.AddRange(new Control[]
        {
            title, prompt, newLabel, _newPassword, confirmLabel, _confirmPassword, _error, okButton, cancelButton
        });
        Controls.Add(panel);

        AcceptButton = okButton;
        CancelButton = cancelButton;
        ActiveControl = _newPassword;
    }

    private static Label MakeFieldLabel(string text, Point location) => new()
    {
        Text = text,
        Font = new Font(ThemeFontName, 11F),
        ForeColor = LabelColor,
        AutoSize = true,
        Location = location
    };

    private ModernTextBox MakeField(Point location) => new()
    {
        Location = location,
        Size = new Size(FieldWidth, 32),
        BackColor = PanelBack,
        ForeColor = LabelColor,
        Font = new Font(ThemeFontName, 12F),
        BorderColor = FieldResting,
        BorderSize = 2,
        UnderlinedStyle = true
    };

    private void OnOk(object? sender, EventArgs e)
    {
        var newPw = _newPassword.Texts.Trim();
        var confirm = _confirmPassword.Texts.Trim();

        if (newPw.Length < MinLength)
        {
            _error.Text = $"Password must be at least {MinLength} characters.";
            return;
        }
        if (newPw != confirm)
        {
            _error.Text = "Passwords do not match.";
            return;
        }

        NewPassword = newPw;
        DialogResult = System.Windows.Forms.DialogResult.OK;
        Close();
    }
}
