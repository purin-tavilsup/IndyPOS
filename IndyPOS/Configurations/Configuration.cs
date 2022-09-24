using IndyPOS.Common.Interfaces;
using IndyPOS.Configurations.Models;
using Serilog;
using System;
using System.Collections.Specialized;
using System.Configuration;
using System.IO;
using System.Threading.Tasks;
using IndyPOS.Facade.Interfaces;

namespace IndyPOS.Configurations
{
	public class Configuration : IConfiguration
	{
		private readonly NameValueCollection _appSettings;
		private readonly IJsonUtility _jsonUtility;
		private readonly ILogger _logger;
		private readonly string _configFilePath;
		private UserConfiguration _userConfiguration;

		public Configuration(IJsonUtility jsonUtility, ILogger logger)
		{
			_appSettings = ConfigurationManager.AppSettings;
			_jsonUtility = jsonUtility;
			_logger = logger;

			var directory = _appSettings.Get("ConfigDirectory");

			_configFilePath = $"{directory}\\UserConfiguration.json";

			LoadUserConfiguration();
		}

		public string ConfigDirectory => _appSettings.Get("ConfigDirectory");

		public string DatabasePath => _appSettings.Get("DatabasePath");

		public string ReportsDirectory => _appSettings.Get("ReportsDirectory");

		public string LogDirectory => _appSettings.Get("LogDirectory");

		public string DataFeedBaseUri => _appSettings.Get("DataFeedBaseUri");

		public string StoreFullName
		{
			get => _userConfiguration.StoreFullName;
			set => _userConfiguration.StoreFullName = value;
		}

		public string StoreName
		{
			get => _userConfiguration.StoreName; 
			set => _userConfiguration.StoreName = value;
		}

		public string StoreAddressLine1
		{
			get => _userConfiguration.StoreAddressLine1; 
			set => _userConfiguration.StoreAddressLine1 = value;
		}

		public string StoreAddressLine2
		{
			get => _userConfiguration.StoreAddressLine2; 
			set => _userConfiguration.StoreAddressLine2 = value;
		}

		public string StorePhoneNumber
		{
			get => _userConfiguration.StorePhoneNumber; 
			set => _userConfiguration.StorePhoneNumber = value;
		}

		public string PrinterName
		{
			get => _userConfiguration.PrinterName; 
			set => _userConfiguration.PrinterName = value;
		}

		public string BarcodeScannerPortName
		{
			get => _userConfiguration.BarcodeScannerPortName; 
			set => _userConfiguration.BarcodeScannerPortName = value;
		}

		public string BackupDbDirectory
		{
			get => _userConfiguration.BackupDbDirectory; 
			set => _userConfiguration.BackupDbDirectory = value;
		}

		public string DataFeedKey
		{
			get => _userConfiguration.DataFeedKey; 
			set => _userConfiguration.DataFeedKey = value;
		}

		public bool DataFeedEnabled
		{
			get => _userConfiguration.DataFeedEnabled; 
			set => _userConfiguration.DataFeedEnabled = value;
		}

		public bool DatabaseBackUpEnabled
		{
			get => _userConfiguration.DatabaseBackUpEnabled;
			set => _userConfiguration.DatabaseBackUpEnabled = value;
		}

		public async Task UpdateAsync()
		{
			await SaveToFileAsync(_userConfiguration);
		}

		private void LoadUserConfiguration()
		{
			_userConfiguration = GetFromFile();
		}

		private void SaveToFile(UserConfiguration configuration)
		{
			try
			{
				_jsonUtility.SaveToFile(configuration, _configFilePath);
			}
			catch (Exception ex)
			{
				_logger.Error(ex, "Failed to save User Configuration to file.");
				throw;
			}
		}

		private async Task SaveToFileAsync(UserConfiguration configuration)
		{
			try
			{
				await _jsonUtility.SaveToFileAsync(configuration, _configFilePath);
			}
			catch (Exception ex)
			{
				_logger.Error(ex, "Failed to save User Configuration to file.");
				throw;
			}
		}

		private UserConfiguration GetFromFile()
		{
			try
			{
				if (!File.Exists(_configFilePath))
				{
					var configuration = CreateNewUserConfiguration();

					SaveToFile(configuration);
				}

				return _jsonUtility.ReadFromFile<UserConfiguration>(_configFilePath);
			}
			catch (Exception ex)
			{
				var errorMessage = $"Failed to get User Configuration from file. {ex.Message}";
				_logger.Error(ex, errorMessage);
				System.Windows.Forms.MessageBox.Show(errorMessage, "Load User Configuration");
				throw;
			}
		}

		private static UserConfiguration CreateNewUserConfiguration()
		{
			const string defaultBackupDbDirectory = @"G:\My Drive\Rungrat\Data\db";

			return new UserConfiguration
			{
				StoreFullName = "รุ่งรัศมิ์",
				StoreName = "รุ่งรัศมิ์",
				StoreAddressLine1 = "134 หมู่ 4 ต.คำชะอี อ.คำชะอี",
				StoreAddressLine2 = "จ.มุกดาหาร 49110",
				StorePhoneNumber = "084-602-9150",
				PrinterName = "XP-58",
				BarcodeScannerPortName = "COM4",
				BackupDbDirectory = defaultBackupDbDirectory,
				DataFeedKey = "dummyKey",
				DataFeedEnabled = false,
				DatabaseBackUpEnabled = false
			};
		}
	}
}
