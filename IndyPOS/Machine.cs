﻿using IndyPOS.Facade.Interfaces;
using IndyPOS.Interfaces;
using IndyPOS.UI;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace IndyPOS;

[ExcludeFromCodeCoverage]
public class Machine : IMachine
{
	private readonly MainForm _mainForm;
	private readonly IStoreConfigurationHelper _storeConfigurationHelper;
	private readonly IBarcodeScannerHelper _barcodeScanner;

	public Machine(MainForm mainForm,
				   IStoreConfigurationHelper storeConfigurationHelper,
				   IBarcodeScannerHelper barcodeScanner)
	{
		_mainForm = mainForm;
		_storeConfigurationHelper = storeConfigurationHelper;
		_barcodeScanner = barcodeScanner;
	}

	public void Dispose()
	{
		Shutdown();
	}

	public void Launch()
	{
		ConnectDevices();
		StartUserInterface();
	}

	private static string GetVersion()
	{
		var assembly = System.Reflection.Assembly.GetExecutingAssembly();
		var versionInfo = FileVersionInfo.GetVersionInfo(assembly.Location);

		return versionInfo.FileVersion ?? "0.0.0";
	}

	private void Shutdown()
	{
		Console.WriteLine("IndyPOS is shutting down...");

		DisconnectDevices();
	}

	[Conditional("RELEASE")]
	private void ConnectDevices()
	{
		_barcodeScanner.Connect();
	}

	[Conditional("RELEASE")]
	private void DisconnectDevices()
	{
		_barcodeScanner?.Disconnect();
	}

	private void StartUserInterface()
	{
		try
		{
			var version = GetVersion();
			var storeConfig = _storeConfigurationHelper.Get();

			_mainForm.SetVersion(version);
			_mainForm.SetStoreName(storeConfig.StoreFullName ?? string.Empty);

			Application.Run(_mainForm);
		}
		catch (Exception ex)
		{
			var messageForm = new MessageForm();
			messageForm.Show($"Error: {ex.Message}", "Unable To Start User Interface!");
		}
	}
}