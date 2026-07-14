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

    // App dark-theme palette (mirrors MessageForm).
    private static readonly Color FormBack = Color.FromArgb(38, 38, 38);
    private static readonly Color PanelBack = Color.FromArgb(30, 30, 30);
    private static readonly Color LabelColor = Color.Gainsboro;
    private static readonly Color AccentTeal = Color.FromArgb(50, 190, 166);
    private static readonly Color AccentRed = Color.FromArgb(224, 79, 95);
    private static readonly Color FieldUnderline = Color.FromArgb(90, 90, 90);
    private const string ThemeFontName = "FC Subject [Non-commercial] Reg";

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
        ClientSize = new Size(460, 300);
        TopMost = true;
        Font = new Font(ThemeFontName, 12F);

        var panel = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = PanelBack,
            BorderStyle = BorderStyle.FixedSingle,
            Padding = new Padding(24)
        };

        var title = new Label
        {
            Text = "Set a New Password",
            Font = new Font(ThemeFontName, 15F, FontStyle.Bold),
            ForeColor = Color.White,
            AutoSize = true,
            Location = new Point(24, 20)
        };

        var prompt = new Label
        {
            Text = "You must set a new password before continuing.",
            Font = new Font(ThemeFontName, 11F),
            ForeColor = LabelColor,
            AutoSize = false,
            Size = new Size(410, 24),
            Location = new Point(24, 58)
        };

        var newLabel = MakeFieldLabel("New password:", new Point(24, 95));
        _newPassword = MakeField(new Point(24, 120));
        _newPassword.PasswordChar = true;

        var confirmLabel = MakeFieldLabel("Confirm password:", new Point(24, 165));
        _confirmPassword = MakeField(new Point(24, 190));
        _confirmPassword.PasswordChar = true;

        _error = new Label
        {
            ForeColor = AccentRed,
            Font = new Font(ThemeFontName, 10F),
            AutoSize = false,
            Size = new Size(410, 22),
            Location = new Point(24, 226),
            TextAlign = ContentAlignment.MiddleLeft
        };

        var okButton = new ModernButton
        {
            Text = "Set Password",
            Size = new Size(160, 44),
            Location = new Point(276, 246),
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
            Size = new Size(120, 44),
            Location = new Point(24, 246),
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
        Size = new Size(410, 32),
        BackColor = PanelBack,
        ForeColor = LabelColor,
        Font = new Font(ThemeFontName, 12F),
        BorderColor = FieldUnderline,
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
