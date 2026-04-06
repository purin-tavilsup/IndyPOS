using IndyPOS.Bootstrapper.Installers;

namespace IndyPOS.Bootstrapper.UI;

/// <summary>
/// Main installation wizard form with progress tracking.
/// </summary>
public partial class InstallationWizard : Form
{
    private readonly InstallationOrchestrator _orchestrator;
    private readonly CancellationTokenSource _cts = new();

    // UI Controls
    private Panel _headerPanel = null!;
    private Label _titleLabel = null!;
    private Label _subtitleLabel = null!;
    private Panel _contentPanel = null!;
    private Label _stepLabel = null!;
    private ProgressBar _progressBar = null!;
    private Label _statusLabel = null!;
    private RichTextBox _logTextBox = null!;
    private Panel _buttonPanel = null!;
    private Button _cancelButton = null!;
    private Button _finishButton = null!;

    // Configuration panel controls
    private Panel _configPanel = null!;
    private TextBox _storeIdTextBox = null!;
    private TextBox _appPasswordTextBox = null!;
    private TextBox _confirmPasswordTextBox = null!;
    private Button _startButton = null!;

    private bool _installationStarted;
    private bool _installationComplete;

    public InstallationWizard()
    {
        _orchestrator = new InstallationOrchestrator();
        InitializeComponents();
        WireEvents();
    }

    private void InitializeComponents()
    {
        // Form settings
        Text = "IndyPOS Setup";
        Size = new Size(600, 500);
        StartPosition = FormStartPosition.CenterScreen;
        FormBorderStyle = FormBorderStyle.FixedSingle;
        MaximizeBox = false;
        BackColor = Color.White;

        // Header panel
        _headerPanel = new Panel
        {
            Dock = DockStyle.Top,
            Height = 80,
            BackColor = Color.FromArgb(45, 45, 48)
        };

        _titleLabel = new Label
        {
            Text = "IndyPOS Setup",
            Font = new Font("Segoe UI", 18, FontStyle.Bold),
            ForeColor = Color.White,
            AutoSize = true,
            Location = new Point(20, 15)
        };

        _subtitleLabel = new Label
        {
            Text = "Point of Sale System Installation",
            Font = new Font("Segoe UI", 10),
            ForeColor = Color.LightGray,
            AutoSize = true,
            Location = new Point(22, 48)
        };

        _headerPanel.Controls.Add(_titleLabel);
        _headerPanel.Controls.Add(_subtitleLabel);

        // Content panel
        _contentPanel = new Panel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(20)
        };

        // Configuration panel (shown first)
        _configPanel = CreateConfigPanel();

        // Progress panel (shown during installation)
        _stepLabel = new Label
        {
            Text = "Ready to install",
            Font = new Font("Segoe UI", 11, FontStyle.Bold),
            AutoSize = true,
            Location = new Point(20, 20),
            Visible = false
        };

        _progressBar = new ProgressBar
        {
            Location = new Point(20, 50),
            Size = new Size(540, 25),
            Style = ProgressBarStyle.Continuous,
            Visible = false
        };

        _statusLabel = new Label
        {
            Text = "",
            Font = new Font("Segoe UI", 9),
            ForeColor = Color.Gray,
            AutoSize = true,
            Location = new Point(20, 80),
            Visible = false
        };

        _logTextBox = new RichTextBox
        {
            Location = new Point(20, 110),
            Size = new Size(540, 200),
            ReadOnly = true,
            BackColor = Color.FromArgb(30, 30, 30),
            ForeColor = Color.LightGreen,
            Font = new Font("Consolas", 9),
            BorderStyle = BorderStyle.None,
            Visible = false
        };

        _contentPanel.Controls.Add(_configPanel);
        _contentPanel.Controls.Add(_stepLabel);
        _contentPanel.Controls.Add(_progressBar);
        _contentPanel.Controls.Add(_statusLabel);
        _contentPanel.Controls.Add(_logTextBox);

        // Button panel
        _buttonPanel = new Panel
        {
            Dock = DockStyle.Bottom,
            Height = 60,
            BackColor = Color.FromArgb(240, 240, 240)
        };

        _cancelButton = new Button
        {
            Text = "Cancel",
            Size = new Size(100, 35),
            Location = new Point(380, 12),
            FlatStyle = FlatStyle.Flat
        };

        _finishButton = new Button
        {
            Text = "Finish",
            Size = new Size(100, 35),
            Location = new Point(485, 12),
            FlatStyle = FlatStyle.Flat,
            BackColor = Color.FromArgb(0, 122, 204),
            ForeColor = Color.White,
            Enabled = false
        };

        _buttonPanel.Controls.Add(_cancelButton);
        _buttonPanel.Controls.Add(_finishButton);

        // Add to form
        Controls.Add(_contentPanel);
        Controls.Add(_headerPanel);
        Controls.Add(_buttonPanel);
    }

    private Panel CreateConfigPanel()
    {
        var panel = new Panel
        {
            Location = new Point(20, 20),
            Size = new Size(540, 300)
        };

        var storeIdLabel = new Label
        {
            Text = "Store ID:",
            Font = new Font("Segoe UI", 10),
            Location = new Point(0, 10),
            AutoSize = true
        };

        _storeIdTextBox = new TextBox
        {
            Location = new Point(0, 35),
            Size = new Size(300, 25),
            Font = new Font("Segoe UI", 10),
            PlaceholderText = "e.g., STORE-001"
        };

        var storeIdHint = new Label
        {
            Text = "Unique identifier for this store location",
            Font = new Font("Segoe UI", 8),
            ForeColor = Color.Gray,
            Location = new Point(0, 62),
            AutoSize = true
        };

        var passwordLabel = new Label
        {
            Text = "Database Password:",
            Font = new Font("Segoe UI", 10),
            Location = new Point(0, 95),
            AutoSize = true
        };

        _appPasswordTextBox = new TextBox
        {
            Location = new Point(0, 120),
            Size = new Size(300, 25),
            Font = new Font("Segoe UI", 10),
            PasswordChar = '*',
            PlaceholderText = "Choose a strong password"
        };

        var confirmLabel = new Label
        {
            Text = "Confirm Password:",
            Font = new Font("Segoe UI", 10),
            Location = new Point(0, 155),
            AutoSize = true
        };

        _confirmPasswordTextBox = new TextBox
        {
            Location = new Point(0, 180),
            Size = new Size(300, 25),
            Font = new Font("Segoe UI", 10),
            PasswordChar = '*',
            PlaceholderText = "Confirm password"
        };

        var passwordHint = new Label
        {
            Text = "This password is used to secure the local database",
            Font = new Font("Segoe UI", 8),
            ForeColor = Color.Gray,
            Location = new Point(0, 207),
            AutoSize = true
        };

        _startButton = new Button
        {
            Text = "Install IndyPOS",
            Size = new Size(150, 40),
            Location = new Point(0, 250),
            FlatStyle = FlatStyle.Flat,
            BackColor = Color.FromArgb(0, 122, 204),
            ForeColor = Color.White,
            Font = new Font("Segoe UI", 10, FontStyle.Bold)
        };

        panel.Controls.AddRange(new Control[]
        {
            storeIdLabel, _storeIdTextBox, storeIdHint,
            passwordLabel, _appPasswordTextBox,
            confirmLabel, _confirmPasswordTextBox, passwordHint,
            _startButton
        });

        return panel;
    }

    private void WireEvents()
    {
        _startButton.Click += StartButton_Click;
        _cancelButton.Click += CancelButton_Click;
        _finishButton.Click += FinishButton_Click;
        FormClosing += InstallationWizard_FormClosing;
    }

    private async void StartButton_Click(object? sender, EventArgs e)
    {
        // Validate inputs
        if (string.IsNullOrWhiteSpace(_storeIdTextBox.Text))
        {
            MessageBox.Show("Please enter a Store ID.", "Validation Error",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
            _storeIdTextBox.Focus();
            return;
        }

        if (string.IsNullOrWhiteSpace(_appPasswordTextBox.Text))
        {
            MessageBox.Show("Please enter a database password.", "Validation Error",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
            _appPasswordTextBox.Focus();
            return;
        }

        if (_appPasswordTextBox.Text != _confirmPasswordTextBox.Text)
        {
            MessageBox.Show("Passwords do not match.", "Validation Error",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
            _confirmPasswordTextBox.Focus();
            return;
        }

        if (_appPasswordTextBox.Text.Length < 8)
        {
            MessageBox.Show("Password must be at least 8 characters.", "Validation Error",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
            _appPasswordTextBox.Focus();
            return;
        }

        // Switch to installation view
        _configPanel.Visible = false;
        _stepLabel.Visible = true;
        _progressBar.Visible = true;
        _statusLabel.Visible = true;
        _logTextBox.Visible = true;
        _startButton.Enabled = false;
        _installationStarted = true;

        var config = new InstallationConfig
        {
            StoreId = _storeIdTextBox.Text.Trim(),
            AppPassword = _appPasswordTextBox.Text
        };

        var progress = new Progress<InstallationProgress>(UpdateProgress);

        try
        {
            await _orchestrator.InstallAsync(config, progress, _cts.Token);

            _installationComplete = true;
            _stepLabel.Text = "Installation Complete!";
            _statusLabel.Text = "IndyPOS has been successfully installed.";
            _progressBar.Value = 100;
            _finishButton.Enabled = true;
            _cancelButton.Enabled = false;

            Log("Installation completed successfully!", Color.LightGreen);
            Log("Click 'Finish' to launch IndyPOS.", Color.White);
        }
        catch (OperationCanceledException)
        {
            _stepLabel.Text = "Installation Cancelled";
            _statusLabel.Text = "The installation was cancelled.";
            Log("Installation cancelled by user.", Color.Yellow);
        }
        catch (Exception ex)
        {
            _stepLabel.Text = "Installation Failed";
            _statusLabel.Text = "An error occurred during installation.";
            Log($"ERROR: {ex.Message}", Color.Red);

            MessageBox.Show(
                $"Installation failed:\n\n{ex.Message}\n\nPlease check the log for details.",
                "Installation Error",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
    }

    private void UpdateProgress(InstallationProgress progress)
    {
        _stepLabel.Text = progress.StepName;
        _statusLabel.Text = progress.StatusMessage;

        if (progress.Percentage >= 0)
        {
            _progressBar.Style = ProgressBarStyle.Continuous;
            _progressBar.Value = Math.Min(progress.Percentage, 100);
        }
        else
        {
            _progressBar.Style = ProgressBarStyle.Marquee;
        }

        if (!string.IsNullOrEmpty(progress.LogMessage))
        {
            Log(progress.LogMessage, progress.IsError ? Color.Red : Color.LightGreen);
        }
    }

    private void Log(string message, Color color)
    {
        if (InvokeRequired)
        {
            Invoke(() => Log(message, color));
            return;
        }

        _logTextBox.SelectionStart = _logTextBox.TextLength;
        _logTextBox.SelectionLength = 0;
        _logTextBox.SelectionColor = color;
        _logTextBox.AppendText($"[{DateTime.Now:HH:mm:ss}] {message}\n");
        _logTextBox.ScrollToCaret();
    }

    private void CancelButton_Click(object? sender, EventArgs e)
    {
        if (_installationStarted && !_installationComplete)
        {
            var result = MessageBox.Show(
                "Are you sure you want to cancel the installation?\n\n" +
                "This may leave the system in an incomplete state.",
                "Cancel Installation",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning);

            if (result == DialogResult.Yes)
            {
                _cts.Cancel();
            }
        }
        else
        {
            Close();
        }
    }

    private void FinishButton_Click(object? sender, EventArgs e)
    {
        // Launch WinForms app
        try
        {
            var winFormsPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "IndyPOS.POS",
                "current",
                "IndyPOS.Windows.Forms.exe");

            if (File.Exists(winFormsPath))
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = winFormsPath,
                    UseShellExecute = true
                });
            }
        }
        catch
        {
            // Ignore launch errors
        }

        Close();
    }

    private void InstallationWizard_FormClosing(object? sender, FormClosingEventArgs e)
    {
        if (_installationStarted && !_installationComplete && !_cts.IsCancellationRequested)
        {
            var result = MessageBox.Show(
                "Installation is in progress. Are you sure you want to cancel?",
                "Cancel Installation",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning);

            if (result == DialogResult.No)
            {
                e.Cancel = true;
                return;
            }

            _cts.Cancel();
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _cts.Dispose();
        }
        base.Dispose(disposing);
    }
}
