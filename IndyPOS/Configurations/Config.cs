using IndyPOS.Common.Interfaces;
using IndyPOS.Configurations.Models;
using IndyPOS.Facade.Interfaces;
using Microsoft.Extensions.Configuration;
using Serilog;
using System.IO;

namespace IndyPOS.Configurations
{
	public class Config : IConfig
	{
		private readonly IJsonUtility _jsonUtility;
		private readonly ILogger _logger;
		private readonly string _userConfigPath;
		private readonly IConfiguration _configuration;
		private readonly UserConfiguration _userConfig;

		public Config(IConfiguration configuration, IJsonUtility jsonUtility, ILogger logger)
		{
			_configuration = configuration;
			_jsonUtility = jsonUtility;
			_logger = logger;
			_userConfigPath = _configuration.GetValue<string>("UserConfigPath") ?? @"C:\\ProgramData\\IndyPOS\\Config\\UserConfiguration.json";
			_userConfig = GetFromFile();
		}

		public string DatabasePath => _configuration.GetValue<string>("DatabasePath") ?? @"C:\\ProgramData\\IndyPOS\\db\\Store.db";

		public string ReportsDirectory => _configuration.GetValue<string>("ReportsDirectory") ?? @"C:\\ProgramData\\IndyPOS\\Reports";

		public string LogDirectory => _configuration.GetValue<string>("LogDirectory") ?? @"C:\\ProgramData\\IndyPOS\\Logs";

		public string DataFeedBaseUri => _configuration.GetValue<string>("DataFeedBaseUri") ?? @"https://rungratdatafeed.azurewebsites.net/api/datafeed/";

		public string StoreFullName
		{
			get => _userConfig.StoreFullName;
			set => _userConfig.StoreFullName = value;
		}

		public string StoreName
		{
			get => _userConfig.StoreName; 
			set => _userConfig.StoreName = value;
		}

		public string StoreAddressLine1
		{
			get => _userConfig.StoreAddressLine1; 
			set => _userConfig.StoreAddressLine1 = value;
		}

		public string StoreAddressLine2
		{
			get => _userConfig.StoreAddressLine2; 
			set => _userConfig.StoreAddressLine2 = value;
		}

		public string StorePhoneNumber
		{
			get => _userConfig.StorePhoneNumber; 
			set => _userConfig.StorePhoneNumber = value;
		}

		public string PrinterName
		{
			get => _userConfig.PrinterName; 
			set => _userConfig.PrinterName = value;
		}

		public string BarcodeScannerPortName
		{
			get => _userConfig.BarcodeScannerPortName; 
			set => _userConfig.BarcodeScannerPortName = value;
		}

		public string BackupDbDirectory
		{
			get => _userConfig.BackupDbDirectory; 
			set => _userConfig.BackupDbDirectory = value;
		}

		public string DataFeedKey
		{
			get => _userConfig.DataFeedKey; 
			set => _userConfig.DataFeedKey = value;
		}

		public bool DataFeedEnabled
		{
			get => _userConfig.DataFeedEnabled; 
			set => _userConfig.DataFeedEnabled = value;
		}

		public bool DatabaseBackUpEnabled
		{
			get => _userConfig.DatabaseBackUpEnabled;
			set => _userConfig.DatabaseBackUpEnabled = value;
		}

		public async Task UpdateAsync()
		{
			await SaveToFileAsync(_userConfig);
		}

		private void SaveToFile(UserConfiguration configuration)
		{
			try
			{
				_jsonUtility.SaveToFile(configuration, _userConfigPath);
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
				await _jsonUtility.SaveToFileAsync(configuration, _userConfigPath);
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
				if (!File.Exists(_userConfigPath))
				{
					var configuration = CreateNewUserConfiguration();

					SaveToFile(configuration);
				}

				return _jsonUtility.ReadFromFile<UserConfiguration>(_userConfigPath);
			}
			catch (Exception ex)
			{
				var errorMessage = $"Failed to get User Configuration from file. {ex.Message}";
				_logger.Error(ex, errorMessage);
				MessageBox.Show(errorMessage, "Load User Configuration");
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
