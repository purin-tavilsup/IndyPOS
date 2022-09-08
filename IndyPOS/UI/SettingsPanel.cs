using IndyPOS.Common.Interfaces;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace IndyPOS.UI
{
	[ExcludeFromCodeCoverage]
    public partial class SettingsPanel : UserControl
    {
		private readonly IConfiguration _configuration;

        public SettingsPanel(IConfiguration configuration)
		{
			_configuration = configuration;

            InitializeComponent();
		}

        private void LoadSettings()
		{
			StoreFullNameTextBox.Texts = _configuration.StoreFullName;
			StoreNameTextBox.Texts = _configuration.StoreName;
			StoreAddressLine1TextBox.Texts = _configuration.StoreAddressLine1;
			StoreAddressLine2TextBox.Texts = _configuration.StoreAddressLine2;
			StorePhoneTextBox.Texts = _configuration.StorePhoneNumber;
			ReceiptPrinterNameTextBox.Texts = _configuration.PrinterName;
			BarcodeScannerPortNameTextBox.Texts = _configuration.BarcodeScannerPortName;
			BackupDBDirectoryTextBox.Texts = _configuration.BackupDbDirectory;
			DataFeedKeyTextBox.Texts = _configuration.DataFeedKey;
			DataFeedEnabled.Checked = _configuration.DataFeedEnabled;
			DatabaseBackUpEnabled.Checked = _configuration.DatabaseBackUpEnabled;
		}

		private async Task SaveSettings()
		{
			_configuration.StoreFullName = StoreFullNameTextBox.Texts.Trim();
			_configuration.StoreName = StoreNameTextBox.Texts.Trim();
			_configuration.StoreAddressLine1 = StoreAddressLine1TextBox.Texts.Trim();
			_configuration.StoreAddressLine2 = StoreAddressLine2TextBox.Texts.Trim();
			_configuration.StorePhoneNumber = StorePhoneTextBox.Texts.Trim();
			_configuration.PrinterName = ReceiptPrinterNameTextBox.Texts.Trim();
			_configuration.BarcodeScannerPortName = BarcodeScannerPortNameTextBox.Texts.Trim();
			_configuration.BackupDbDirectory = BackupDBDirectoryTextBox.Texts.Trim();
			_configuration.DataFeedKey = DataFeedKeyTextBox.Texts.Trim();
			_configuration.DataFeedEnabled = DataFeedEnabled.Checked;
			_configuration.DatabaseBackUpEnabled = DatabaseBackUpEnabled.Checked;

			await _configuration.UpdateAsync();
		}

        private async void SaveSettingsButton_Click(object sender, EventArgs e)
        {
			await SaveSettings();
        }

        private void SettingsPanel_VisibleChanged(object sender, EventArgs e)
        {
			if (!Visible)
				return;

			LoadSettings();
        }
    }
}
