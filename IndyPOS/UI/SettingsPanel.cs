using System;
using System.Diagnostics.CodeAnalysis;
using System.Windows.Forms;

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

        public void LoadSettings()
		{
			StoreNameTextBox.Texts = _config.StoreName;
			StoreAddressLine1TextBox.Texts = _config.StoreAddressLine1;
			StoreAddressLine2TextBox.Texts = _config.StoreAddressLine2;
			StorePhoneTextBox.Texts = _config.StorePhoneNumber;

			ReceiptPrinterNameTextBox.Texts = _config.PrinterName;
		}

		private void SaveSettings()
		{
			_config.StoreName = StoreNameTextBox.Texts.Trim();
			_config.StoreAddressLine1 = StoreAddressLine1TextBox.Texts.Trim();
			_config.StoreAddressLine2 = StoreAddressLine2TextBox.Texts.Trim();
			_config.StorePhoneNumber = StorePhoneTextBox.Texts.Trim();

			_config.PrinterName = ReceiptPrinterNameTextBox.Texts.Trim();
		}

        private void SaveSettingsButton_Click(object sender, EventArgs e)
        {
			SaveSettings();
        }

        private void SettingsPanel_VisibleChanged(object sender, EventArgs e)
        {
			if (!Visible)
				return;

			LoadSettings();
        }
    }
}
