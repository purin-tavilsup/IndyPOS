using IndyPOS.Application.Common.Interfaces;
using IndyPOS.Application.Common.Models;
using Serilog;
using System.Diagnostics.CodeAnalysis;
using System.Net.Http;

namespace IndyPOS.Windows.Forms.UI.Setup;

/// <summary>
/// First-run wizard for initial store configuration.
/// Triggered after Velopack installation via Program.IsFirstRun.
/// </summary>
[ExcludeFromCodeCoverage]
public partial class FirstRunWizard : Form
{
    private readonly IStoreConfigurationService _storeConfigurationService;
    private int _currentStep;
    private const int TotalSteps = 3;

    // Step 1: Store Info
    private Panel _storeInfoPanel = null!;
    private TextBox _storeFullNameTextBox = null!;
    private TextBox _storeNameTextBox = null!;
    private TextBox _addressLine1TextBox = null!;
    private TextBox _addressLine2TextBox = null!;
    private TextBox _phoneTextBox = null!;

    // Step 2: Hardware
    private Panel _hardwarePanel = null!;
    private TextBox _printerNameTextBox = null!;
    private TextBox _serialPortTextBox = null!;
    private TextBox _cashDrawerCodeTextBox = null!;

    // Step 3: Connection Test
    private Panel _connectionPanel = null!;
    private Label _connectionStatusLabel = null!;
    private Button _testConnectionButton = null!;

    // Navigation
    private Button _backButton = null!;
    private Button _nextButton = null!;
    private Label _stepLabel = null!;

    public bool ConfigurationComplete { get; private set; }

    public FirstRunWizard(IStoreConfigurationService storeConfigurationService)
    {
        _storeConfigurationService = storeConfigurationService;
        _currentStep = 1;

        InitializeComponent();
        CreateWizardPanels();
        ShowCurrentStep();
    }

    private void InitializeComponent()
    {
        SuspendLayout();

        Text = "IndyPOS - First Run Setup";
        Size = new Size(600, 500);
        StartPosition = FormStartPosition.CenterScreen;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        BackColor = Color.FromArgb(30, 30, 30);

        // Title
        var titleLabel = new Label
        {
            Text = "Welcome to IndyPOS",
            Font = new Font("Segoe UI", 18, FontStyle.Bold),
            ForeColor = Color.White,
            Location = new Point(20, 20),
            AutoSize = true
        };
        Controls.Add(titleLabel);

        // Step indicator
        _stepLabel = new Label
        {
            Font = new Font("Segoe UI", 10),
            ForeColor = Color.Gray,
            Location = new Point(20, 55),
            AutoSize = true
        };
        Controls.Add(_stepLabel);

        // Navigation buttons
        _backButton = new Button
        {
            Text = "← Back",
            Size = new Size(100, 40),
            Location = new Point(260, 400),
            FlatStyle = FlatStyle.Flat,
            BackColor = Color.FromArgb(60, 60, 60),
            ForeColor = Color.White,
            Font = new Font("Segoe UI", 10)
        };
        _backButton.FlatAppearance.BorderColor = Color.FromArgb(80, 80, 80);
        _backButton.Click += BackButton_Click;
        Controls.Add(_backButton);

        _nextButton = new Button
        {
            Text = "Next →",
            Size = new Size(100, 40),
            Location = new Point(370, 400),
            FlatStyle = FlatStyle.Flat,
            BackColor = Color.FromArgb(0, 120, 215),
            ForeColor = Color.White,
            Font = new Font("Segoe UI", 10)
        };
        _nextButton.FlatAppearance.BorderColor = Color.FromArgb(0, 100, 180);
        _nextButton.Click += NextButton_Click;
        Controls.Add(_nextButton);

        ResumeLayout(false);
    }

    private void CreateWizardPanels()
    {
        CreateStoreInfoPanel();
        CreateHardwarePanel();
        CreateConnectionPanel();
    }

    private void CreateStoreInfoPanel()
    {
        _storeInfoPanel = new Panel
        {
            Location = new Point(20, 90),
            Size = new Size(540, 290),
            BackColor = Color.FromArgb(40, 40, 40)
        };

        var instructionLabel = new Label
        {
            Text = "Enter your store information:",
            Font = new Font("Segoe UI", 11),
            ForeColor = Color.White,
            Location = new Point(10, 10),
            AutoSize = true
        };
        _storeInfoPanel.Controls.Add(instructionLabel);

        // Store Full Name
        AddLabelAndTextBox(_storeInfoPanel, "Store Full Name:", 50, out _storeFullNameTextBox);

        // Store Short Name
        AddLabelAndTextBox(_storeInfoPanel, "Store Short Name:", 100, out _storeNameTextBox);

        // Address Line 1
        AddLabelAndTextBox(_storeInfoPanel, "Address Line 1:", 150, out _addressLine1TextBox);

        // Address Line 2
        AddLabelAndTextBox(_storeInfoPanel, "Address Line 2:", 200, out _addressLine2TextBox);

        // Phone
        AddLabelAndTextBox(_storeInfoPanel, "Phone Number:", 250, out _phoneTextBox);

        Controls.Add(_storeInfoPanel);
    }

    private void CreateHardwarePanel()
    {
        _hardwarePanel = new Panel
        {
            Location = new Point(20, 90),
            Size = new Size(540, 290),
            BackColor = Color.FromArgb(40, 40, 40),
            Visible = false
        };

        var instructionLabel = new Label
        {
            Text = "Configure your hardware (optional):",
            Font = new Font("Segoe UI", 11),
            ForeColor = Color.White,
            Location = new Point(10, 10),
            AutoSize = true
        };
        _hardwarePanel.Controls.Add(instructionLabel);

        // Printer Name
        AddLabelAndTextBox(_hardwarePanel, "Receipt Printer Name:", 50, out _printerNameTextBox);
        _printerNameTextBox.Text = "XP-58";

        // Serial Port
        AddLabelAndTextBox(_hardwarePanel, "Cash Drawer Port:", 100, out _serialPortTextBox);
        _serialPortTextBox.Text = "COM1";

        // Cash Drawer Code
        AddLabelAndTextBox(_hardwarePanel, "Cash Drawer Code:", 150, out _cashDrawerCodeTextBox);
        _cashDrawerCodeTextBox.Text = "1";

        var noteLabel = new Label
        {
            Text = "Note: You can change these settings later in Settings → Configuration.",
            Font = new Font("Segoe UI", 9, FontStyle.Italic),
            ForeColor = Color.Gray,
            Location = new Point(10, 220),
            AutoSize = true
        };
        _hardwarePanel.Controls.Add(noteLabel);

        Controls.Add(_hardwarePanel);
    }

    private void CreateConnectionPanel()
    {
        _connectionPanel = new Panel
        {
            Location = new Point(20, 90),
            Size = new Size(540, 290),
            BackColor = Color.FromArgb(40, 40, 40),
            Visible = false
        };

        var instructionLabel = new Label
        {
            Text = "Test StoreHub Connection:",
            Font = new Font("Segoe UI", 11),
            ForeColor = Color.White,
            Location = new Point(10, 10),
            AutoSize = true
        };
        _connectionPanel.Controls.Add(instructionLabel);

        _testConnectionButton = new Button
        {
            Text = "Test Connection",
            Size = new Size(150, 40),
            Location = new Point(10, 60),
            FlatStyle = FlatStyle.Flat,
            BackColor = Color.FromArgb(60, 60, 60),
            ForeColor = Color.White,
            Font = new Font("Segoe UI", 10)
        };
        _testConnectionButton.FlatAppearance.BorderColor = Color.FromArgb(80, 80, 80);
        _testConnectionButton.Click += TestConnectionButton_Click;
        _connectionPanel.Controls.Add(_testConnectionButton);

        _connectionStatusLabel = new Label
        {
            Text = "Click 'Test Connection' to verify StoreHub is running.",
            Font = new Font("Segoe UI", 10),
            ForeColor = Color.Gray,
            Location = new Point(10, 120),
            Size = new Size(500, 60)
        };
        _connectionPanel.Controls.Add(_connectionStatusLabel);

        var completeLabel = new Label
        {
            Text = "Click 'Finish' to save your configuration and start using IndyPOS.",
            Font = new Font("Segoe UI", 10),
            ForeColor = Color.White,
            Location = new Point(10, 200),
            AutoSize = true
        };
        _connectionPanel.Controls.Add(completeLabel);

        Controls.Add(_connectionPanel);
    }

    private static void AddLabelAndTextBox(Panel panel, string labelText, int yPosition, out TextBox textBox)
    {
        var label = new Label
        {
            Text = labelText,
            Font = new Font("Segoe UI", 10),
            ForeColor = Color.LightGray,
            Location = new Point(10, yPosition),
            Size = new Size(150, 25)
        };
        panel.Controls.Add(label);

        textBox = new TextBox
        {
            Font = new Font("Segoe UI", 10),
            BackColor = Color.FromArgb(50, 50, 50),
            ForeColor = Color.White,
            BorderStyle = BorderStyle.FixedSingle,
            Location = new Point(170, yPosition - 2),
            Size = new Size(350, 28)
        };
        panel.Controls.Add(textBox);
    }

    private void ShowCurrentStep()
    {
        _storeInfoPanel.Visible = _currentStep == 1;
        _hardwarePanel.Visible = _currentStep == 2;
        _connectionPanel.Visible = _currentStep == 3;

        _backButton.Enabled = _currentStep > 1;
        _nextButton.Text = _currentStep == TotalSteps ? "Finish ✓" : "Next →";

        _stepLabel.Text = $"Step {_currentStep} of {TotalSteps}";
    }

    private void BackButton_Click(object? sender, EventArgs e)
    {
        if (_currentStep > 1)
        {
            _currentStep--;
            ShowCurrentStep();
        }
    }

    private async void NextButton_Click(object? sender, EventArgs e)
    {
        if (_currentStep < TotalSteps)
        {
            _currentStep++;
            ShowCurrentStep();
        }
        else
        {
            await SaveConfigurationAsync();
        }
    }

    private async void TestConnectionButton_Click(object? sender, EventArgs e)
    {
        _testConnectionButton.Enabled = false;
        _connectionStatusLabel.Text = "Testing connection...";
        _connectionStatusLabel.ForeColor = Color.Yellow;

        try
        {
            using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(5) };
            var response = await client.GetAsync("http://localhost:5000/health");

            if (response.IsSuccessStatusCode)
            {
                _connectionStatusLabel.Text = "✓ StoreHub is running and healthy!";
                _connectionStatusLabel.ForeColor = Color.LightGreen;
            }
            else
            {
                _connectionStatusLabel.Text = $"⚠ StoreHub responded with status: {response.StatusCode}";
                _connectionStatusLabel.ForeColor = Color.Orange;
            }
        }
        catch (HttpRequestException)
        {
            _connectionStatusLabel.Text = "✗ Could not connect to StoreHub.\nMake sure the service is running.";
            _connectionStatusLabel.ForeColor = Color.Salmon;
        }
        catch (TaskCanceledException)
        {
            _connectionStatusLabel.Text = "✗ Connection timed out.";
            _connectionStatusLabel.ForeColor = Color.Salmon;
        }
        finally
        {
            _testConnectionButton.Enabled = true;
        }
    }

    private async Task SaveConfigurationAsync()
    {
        try
        {
            _nextButton.Enabled = false;
            _nextButton.Text = "Saving...";

            if (!int.TryParse(_cashDrawerCodeTextBox.Text.Trim(), out var cashDrawerCode))
            {
                cashDrawerCode = 1;
            }

            var config = new StoreConfiguration
            {
                StoreFullName = _storeFullNameTextBox.Text.Trim(),
                StoreName = _storeNameTextBox.Text.Trim(),
                StoreAddressLine1 = _addressLine1TextBox.Text.Trim(),
                StoreAddressLine2 = _addressLine2TextBox.Text.Trim(),
                StorePhoneNumber = _phoneTextBox.Text.Trim(),
                PrinterName = _printerNameTextBox.Text.Trim(),
                SerialPortName = _serialPortTextBox.Text.Trim(),
                Code = cashDrawerCode
            };

            await _storeConfigurationService.UpdateAsync(config);

            Log.Information("First-run wizard completed. Store configuration saved.");
            ConfigurationComplete = true;
            DialogResult = System.Windows.Forms.DialogResult.OK;
            Close();
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to save store configuration in first-run wizard");
            var messageForm = new MessageForm();
            messageForm.ShowDialog($"Error: {ex.Message}", "Unable To Save Configuration!");

            _nextButton.Enabled = true;
            _nextButton.Text = "Finish ✓";
        }
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        // If user closes without completing, ask for confirmation
        if (!ConfigurationComplete && e.CloseReason == CloseReason.UserClosing)
        {
            var result = MessageBox.Show(
                "Are you sure you want to skip the setup wizard?\nYou can configure settings later from the Settings panel.",
                "Skip Setup?",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (result == System.Windows.Forms.DialogResult.No)
            {
                e.Cancel = true;
                return;
            }

            Log.Information("User skipped first-run wizard");
        }

        base.OnFormClosing(e);
    }
}
