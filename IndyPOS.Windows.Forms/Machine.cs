using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using IndyPOS.Application.Interfaces;
using IndyPOS.Windows.Forms.Interfaces;
using IndyPOS.Windows.Forms.UI;

namespace IndyPOS.Windows.Forms;

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
		try
		{
			ConnectDevices();
			StartUserInterface();
		}
		catch (Exception ex)
		{
			var messageForm = new MessageForm();
			messageForm.Show($"Error: {ex.Message}", "Unexpected error has occurred!");
		}
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
		_barcodeScanner.Disconnect();
	}

	private void StartUserInterface()
	{
		var version = GetVersion();
		var storeConfig = _storeConfigurationHelper.Get();

		_mainForm.SetVersion(version);
		_mainForm.SetStoreName(storeConfig.StoreFullName ?? string.Empty);

		System.Windows.Forms.Application.Run(_mainForm);
	}
}