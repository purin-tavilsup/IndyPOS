using IndyPOS.Common.Interfaces;
using IndyPOS.Facade.Interfaces;
using IndyPOS.Interfaces;
using IndyPOS.UI;
using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace IndyPOS
{
	[ExcludeFromCodeCoverage]
    public class Machine : IMachine
	{
        private readonly MainForm _mainForm;
		private readonly IConfiguration _configuration;
        private readonly IBarcodeScannerHelper _barcodeScanner;

		public Machine(MainForm mainForm,
					   IConfiguration configuration,
					   IBarcodeScannerHelper barcodeScanner)
        {
            _mainForm = mainForm;
			_configuration = configuration;
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

			return versionInfo.FileVersion;
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
			var version = GetVersion();

			_mainForm.SetVersion(version);
			_mainForm.SetStoreName(_configuration.StoreFullName);

            System.Windows.Forms.Application.Run(_mainForm);
        }
	}
}
