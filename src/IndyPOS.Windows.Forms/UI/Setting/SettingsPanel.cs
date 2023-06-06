using IndyPOS.Application.Common.Interfaces;
using IndyPOS.Application.Common.Models;
using IndyPOS.Application.Events;
using Prism.Events;
using System.Diagnostics.CodeAnalysis;
using IndyPOS.Application.Common.Enums;

namespace IndyPOS.Windows.Forms.UI.Setting;

[ExcludeFromCodeCoverage]
public partial class SettingsPanel : UserControl
{
    private readonly IStoreConfigurationService _storeConfigurationService;
    private readonly IEventAggregator _eventAggregator;
    private readonly IRawInputDeviceService _rawInputDeviceService;

    public SettingsPanel(IStoreConfigurationService storeConfigurationService,
                         IEventAggregator eventAggregator,
                         IRawInputDeviceService rawInputDeviceService)
    {
        _storeConfigurationService = storeConfigurationService;
        _eventAggregator = eventAggregator;
        _rawInputDeviceService = rawInputDeviceService;

        InitializeComponent();
        SubscribeEvents();
    }

    private void SubscribeEvents()
    {
        _eventAggregator.GetEvent<RawInputDeviceNameReceivedEvent>().Subscribe(RawInputDeviceNameReceived);
    }

    private async Task LoadSettingsAsync()
    {
        try
        {
            var config = await _storeConfigurationService.GetAsync();

            StoreFullNameTextBox.Texts = config.StoreFullName ?? string.Empty;
            StoreNameTextBox.Texts = config.StoreName ?? string.Empty;
            StoreAddressLine1TextBox.Texts = config.StoreAddressLine1 ?? string.Empty;
            StoreAddressLine2TextBox.Texts = config.StoreAddressLine2 ?? string.Empty;
            StorePhoneTextBox.Texts = config.StorePhoneNumber ?? string.Empty;
            ReceiptPrinterNameTextBox.Texts = config.PrinterName ?? string.Empty;
            BarcodeScannerDeviceNameTextBox.Texts = config.BarcodeScannerDeviceName ?? string.Empty;
        }
        catch (Exception ex)
        {
            var messageForm = new MessageForm();
            messageForm.ShowDialog($"Error: {ex.Message}", "Unable To Get Store Configuration!");
        }
    }

    private async Task SaveSettings()
    {
        try
        {
            var config = new StoreConfiguration
            {
                StoreFullName = StoreFullNameTextBox.Texts.Trim(),
                StoreName = StoreNameTextBox.Texts.Trim(),
                StoreAddressLine1 = StoreAddressLine1TextBox.Texts.Trim(),
                StoreAddressLine2 = StoreAddressLine2TextBox.Texts.Trim(),
                StorePhoneNumber = StorePhoneTextBox.Texts.Trim(),
                PrinterName = ReceiptPrinterNameTextBox.Texts.Trim(),
                BarcodeScannerDeviceName = BarcodeScannerDeviceNameTextBox.Texts.Trim()
            };

            await _storeConfigurationService.UpdateAsync(config);
        }
        catch (Exception ex)
        {
            var messageForm = new MessageForm();
            messageForm.ShowDialog($"Error: {ex.Message}", "Unable To Update Store Configuration!");
        }
    }

    private async void SaveSettingsButton_Click(object sender, EventArgs e)
    {
        await SaveSettings();

        if (BarcodeScannerDeviceNameTextBox.Texts.Length > 0)
        {
            _rawInputDeviceService.LoadConfiguration();
        }
    }

    private async void SettingsPanel_VisibleChanged(object sender, EventArgs e)
    {
        if (!Visible)
            return;

        await LoadSettingsAsync();
    }

    private void RawInputDeviceNameReceived(string deviceName)
    {
        BarcodeScannerDeviceNameTextBox.Texts = deviceName;
    }

    private void AddBarcodeScannerButton_Click(object sender, EventArgs e)
    {
        BarcodeScannerDeviceNameTextBox.Texts = string.Empty;

        _rawInputDeviceService.SetMode(RawInputDeviceMode.GetDeviceName);
    }
}