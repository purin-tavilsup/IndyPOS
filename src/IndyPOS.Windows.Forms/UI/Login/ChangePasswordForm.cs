namespace IndyPOS.Windows.Forms.UI.Login;

/// <summary>
/// Modal shown on first login when the admin bootstrap password must be rotated.
/// Asks for a new password + confirmation only; the current (bootstrap) password
/// is supplied by the coordinator.
/// </summary>
public class ChangePasswordForm : Form
{
    private const int MinLength = 8;

    private readonly TextBox _newPassword = new() { PasswordChar = '*', Width = 260 };
    private readonly TextBox _confirmPassword = new() { PasswordChar = '*', Width = 260 };
    private readonly Label _error = new() { ForeColor = Color.Red, AutoSize = true, MaximumSize = new Size(300, 0) };

    /// <summary>The accepted new password, or null if the dialog was cancelled.</summary>
    public string? NewPassword { get; private set; }

    public ChangePasswordForm()
    {
        Text = "Set a New Password";
        FormBorderStyle = FormBorderStyle.FixedDialog;
        StartPosition = FormStartPosition.CenterParent;
        MaximizeBox = false;
        MinimizeBox = false;
        ClientSize = new Size(340, 240);

        var prompt = new Label
        {
            Text = "You must set a new password before continuing.",
            AutoSize = true, MaximumSize = new Size(300, 0), Location = new Point(20, 15)
        };
        var newLabel = new Label { Text = "New password:", AutoSize = true, Location = new Point(20, 55) };
        _newPassword.Location = new Point(20, 78);
        var confirmLabel = new Label { Text = "Confirm password:", AutoSize = true, Location = new Point(20, 110) };
        _confirmPassword.Location = new Point(20, 133);
        _error.Location = new Point(20, 165);

        var ok = new Button { Text = "Set Password", Location = new Point(150, 200), Size = new Size(110, 30) };
        ok.Click += OnOk;
        var cancel = new Button { Text = "Cancel", Location = new Point(20, 200), Size = new Size(90, 30), DialogResult = System.Windows.Forms.DialogResult.Cancel };

        Controls.AddRange(new Control[]
        {
            prompt, newLabel, _newPassword, confirmLabel, _confirmPassword, _error, ok, cancel
        });
        AcceptButton = ok;
        CancelButton = cancel;
    }

    private void OnOk(object? sender, EventArgs e)
    {
        var newPw = _newPassword.Text.Trim();
        var confirm = _confirmPassword.Text.Trim();

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
