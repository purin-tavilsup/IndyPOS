using IndyPOS.Devices;
using IndyPOS.UI;
using System.IO;

namespace IndyPOS
{
    public class Machine : IMachine
	{
		private const string Version = "1.0.11";
        private readonly MainForm _mainForm;
		private readonly IConfig _config;
        private readonly IBarcodeScanner _barcodeScanner;

        public Machine(MainForm mainForm, IConfig config, IBarcodeScanner barcodeScanner)
        {
            _mainForm = mainForm;
			_config = config;
            _barcodeScanner = barcodeScanner;
        }

		public void Dispose()
		{
            Shutdown();
		}

		public void Launch()
		{
			LoadConfig();
            ConnectDevices();
            StartUserInterface();
        }

		private void LoadConfig()
		{
			const string directoryPath = @"C:\ProgramData\IndyPOS\Config";
			const string defaultReportDirectory = @"C:\ProgramData\IndyPOS\Report";
			const string defaultBackupDbDirectory = @"C:\ProgramData\IndyPOS\BackupDB";
			const string defaultBarcodeDirectory = @"C:\ProgramData\IndyPOS\Barcodes";
			
			if (!Directory.Exists(directoryPath))
			{
				Directory.CreateDirectory(directoryPath);
			}

			const string filePath = directoryPath + @"\application.config";

			_config.FileName = filePath;
			
			if (File.Exists(filePath))
			{
				_config.Load();
				_config.Save();
			}
			else
			{
				// Set default values for now
				_config.StoreFullName = "รุ่งรัศมิ์";
				_config.StoreName = "รุ่งรัศมิ์";
				_config.StoreAddressLine1 = "134 หมู่ 4 ต.คำชะอี อ.คำชะอี";
				_config.StoreAddressLine2 = "จ.มุกดาหาร 49110";
                _config.StorePhoneNumber = "084-602-9150";
				_config.PrinterName = "XP-58";
				_config.BarcodeScannerPortName = "COM4";
				_config.ReportDirectory = defaultReportDirectory;
				_config.BackupDbDirectory = defaultBackupDbDirectory;
				_config.BarcodeDirectory = defaultBarcodeDirectory;

                _config.Save();
				_config.Load();
			}
		}

        public void Shutdown()
		{
            DisconnectDevices();
        }

        private void ConnectDevices()
		{
            _barcodeScanner.Connect();
        }

        private void DisconnectDevices()
		{
            _barcodeScanner?.Disconnect();
		}

        private void StartUserInterface()
        {
			_mainForm.SetVersion(Version);
			_mainForm.SetStoreName(_config.StoreFullName);

            System.Windows.Forms.Application.Run(_mainForm);
        }
    }
}
