using IndyPOS.Common.Interfaces;
using System.Diagnostics.CodeAnalysis;

namespace IndyPOS.UI
{
	[ExcludeFromCodeCoverage]
    public partial class SettingsPanel : UserControl
    {
		private readonly IConfig _config;

        public SettingsPanel(IConfig config)
		{
			_config = config;

            InitializeComponent();
		}

        private void LoadSettings()
		{
			StoreFullNameTextBox.Texts = _config.StoreFullName;
			StoreNameTextBox.Texts = _config.StoreName;
			StoreAddressLine1TextBox.Texts = _config.StoreAddressLine1;
			StoreAddressLine2TextBox.Texts = _config.StoreAddressLine2;
			StorePhoneTextBox.Texts = _config.StorePhoneNumber;
			ReceiptPrinterNameTextBox.Texts = _config.PrinterName;
			BarcodeScannerPortNameTextBox.Texts = _config.BarcodeScannerPortName;
			BackupDBDirectoryTextBox.Texts = _config.BackupDbDirectory;
			DataFeedKeyTextBox.Texts = _config.DataFeedKey;
			DataFeedEnabled.Checked = _config.DataFeedEnabled;
			DatabaseBackUpEnabled.Checked = _config.DatabaseBackUpEnabled;
		}

		private async Task SaveSettings()
		{
			_config.StoreFullName = StoreFullNameTextBox.Texts.Trim();
			_config.StoreName = StoreNameTextBox.Texts.Trim();
			_config.StoreAddressLine1 = StoreAddressLine1TextBox.Texts.Trim();
			_config.StoreAddressLine2 = StoreAddressLine2TextBox.Texts.Trim();
			_config.StorePhoneNumber = StorePhoneTextBox.Texts.Trim();
			_config.PrinterName = ReceiptPrinterNameTextBox.Texts.Trim();
			_config.BarcodeScannerPortName = BarcodeScannerPortNameTextBox.Texts.Trim();
			_config.BackupDbDirectory = BackupDBDirectoryTextBox.Texts.Trim();
			_config.DataFeedKey = DataFeedKeyTextBox.Texts.Trim();
			_config.DataFeedEnabled = DataFeedEnabled.Checked;
			_config.DatabaseBackUpEnabled = DatabaseBackUpEnabled.Checked;

			await _config.UpdateAsync();
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
