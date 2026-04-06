using IndyPOS.Application.Common.Interfaces;
using IndyPOS.Windows.Forms.Interfaces;
using IndyPOS.Windows.Forms.Services;
using IndyPOS.Windows.Forms.UI;
using IndyPOS.Windows.Forms.UI.Setup;
using Serilog;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace IndyPOS.Windows.Forms;

[ExcludeFromCodeCoverage]
public class Machine : IMachine
{
	private readonly MainForm _mainForm;
	private readonly IStoreConfigurationService _storeConfigurationService;
	private readonly IRawInputDeviceService _rawInputDeviceService;
	private readonly Func<FirstRunWizard> _firstRunWizardFactory;
	private readonly IUpdateService _updateService;

	public Machine(MainForm mainForm,
				   IStoreConfigurationService storeConfigurationService,
				   IRawInputDeviceService rawInputDeviceService,
				   Func<FirstRunWizard> firstRunWizardFactory,
				   IUpdateService updateService)
	{
		_mainForm = mainForm;
		_storeConfigurationService = storeConfigurationService;
		_rawInputDeviceService = rawInputDeviceService;
		_firstRunWizardFactory = firstRunWizardFactory;
		_updateService = updateService;

		// Subscribe to update notifications
		_updateService.UpdateAvailable += OnUpdateAvailable;
	}

	public void Dispose()
	{
		Shutdown();
	}

	public void Launch()
	{
		try
		{
			// Check if this is first run after Velopack installation
			if (Program.IsFirstRun)
			{
				Log.Information("First run detected, showing setup wizard");
				ShowFirstRunWizard();
			}

			// Check for updates in the background (non-blocking)
			_ = CheckForUpdatesAsync();

			_rawInputDeviceService.Start(_mainForm.Handle);

			StartUserInterface();
		}
		catch (Exception ex)
		{
			var messageForm = new MessageForm();
			messageForm.ShowDialog($"Error: {ex.Message}", "Unexpected error has occurred!");
		}
	}

	private async Task CheckForUpdatesAsync()
	{
		try
		{
			await _updateService.CheckForUpdatesAsync();
		}
		catch (Exception ex)
		{
			// Log but don't block - update check is not critical
			Log.Warning(ex, "Background update check failed");
		}
	}

	private void OnUpdateAvailable(object? sender, UpdateAvailableEventArgs e)
	{
		// Show update notification to user (thread-safe)
		if (_mainForm.InvokeRequired)
		{
			_mainForm.Invoke(() => ShowUpdateNotification(e.Version));
		}
		else
		{
			ShowUpdateNotification(e.Version);
		}
	}

	private void ShowUpdateNotification(string version)
	{
		var result = MessageBox.Show(
			$"A new version ({version}) is available.\n\nWould you like to update now?\n\nThe application will restart after the update.",
			"Update Available",
			MessageBoxButtons.YesNo,
			MessageBoxIcon.Information);

		if (result == System.Windows.Forms.DialogResult.Yes)
		{
			ApplyUpdateAsync();
		}
	}

	private async void ApplyUpdateAsync()
	{
		try
		{
			// Show a simple message that we're updating
			var messageForm = new MessageForm();
			_ = Task.Run(() => messageForm.ShowDialog("Downloading update... Please wait.", "Updating IndyPOS"));

			await _updateService.ApplyUpdateAsync();
			// If we get here, the restart didn't happen (shouldn't normally occur)
		}
		catch (Exception ex)
		{
			Log.Error(ex, "Failed to apply update");
			var errorForm = new MessageForm();
			errorForm.ShowDialog($"Failed to apply update: {ex.Message}", "Update Error");
		}
	}

	private void ShowFirstRunWizard()
	{
		try
		{
			using var wizard = _firstRunWizardFactory();
			wizard.ShowDialog();
		}
		catch (Exception ex)
		{
			Log.Error(ex, "Failed to show first-run wizard");
		}
	}

	private static string GetVersion()
	{
		return Application.Common.AppVersion.GetVersionInfo().DisplayVersion;
	}

	private void Shutdown()
	{
		Console.WriteLine("IndyPOS is shutting down...");

		_rawInputDeviceService.Stop();
	}

	private void StartUserInterface()
	{
		var version = GetVersion();
		var storeConfig = _storeConfigurationService.Get();

		_mainForm.AutoScroll = true;
		_mainForm.SetVersion(version);
		_mainForm.SetStoreName(storeConfig.StoreFullName ?? string.Empty);

		System.Windows.Forms.Application.Run(_mainForm);
	}
}