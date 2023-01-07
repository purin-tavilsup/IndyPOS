﻿using IndyPOS.Application.Common.Interfaces;
using IndyPOS.Application.Common.Models;
using System.Diagnostics.CodeAnalysis;

namespace IndyPOS.Windows.Forms.UI.Setting;

[ExcludeFromCodeCoverage]
public partial class SettingsPanel : UserControl
{
	private readonly IStoreConfigurationService _storeConfigurationService;

	public SettingsPanel(IStoreConfigurationService storeConfigurationService)
	{
		_storeConfigurationService = storeConfigurationService;

		InitializeComponent();
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
			BarcodeScannerPortNameTextBox.Texts = config.BarcodeScannerPortName ?? string.Empty;
		}
		catch (Exception ex)
		{
			var messageForm = new MessageForm();
			messageForm.Show($"Error: {ex.Message}", "Unable To Get Store Configuration!");
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
				BarcodeScannerPortName = BarcodeScannerPortNameTextBox.Texts.Trim()
			};

			await _storeConfigurationService.UpdateAsync(config);
		}
		catch (Exception ex)
		{
			var messageForm = new MessageForm();
			messageForm.Show($"Error: {ex.Message}", "Unable To Update Store Configuration!");
		}
	}

	private async void SaveSettingsButton_Click(object sender, EventArgs e)
	{
		await SaveSettings();
	}

	private async void SettingsPanel_VisibleChanged(object sender, EventArgs e)
	{
		if (!Visible)
			return;

		await LoadSettingsAsync();
	}
}