using IndyPOS.Facade.Interfaces;
using IndyPOS.Facade.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace IndyPOS.Facade.Helpers;

public class StoreConfigurationHelper : IStoreConfigurationHelper
{
	private readonly IJsonUtility _jsonUtility;
	private readonly ILogger<StoreConfigurationHelper> _logger;
	private readonly string _storeConfigPath;

	public StoreConfigurationHelper(IConfiguration configuration, IJsonUtility jsonUtility, ILogger<StoreConfigurationHelper> logger)
	{
		_jsonUtility = jsonUtility;
		_logger = logger;
		_storeConfigPath = GetStoreConfigurationPath(configuration);
	}

	public async Task<StoreConfiguration> GetAsync()
	{
		return await GetFromFileAsync();
	}

	public StoreConfiguration Get()
	{
		return  GetFromFile();
	}

	public async Task UpdateAsync(StoreConfiguration configuration)
	{
		await SaveToFileAsync(configuration);
	}

	private static string GetStoreConfigurationPath(IConfiguration configuration)
	{
		var path = configuration.GetValue<string>("Store:ConfigPath");

		return path ?? "C:\\ProgramData\\IndyPOS\\Config\\StoreConfiguration.json";
	}

	private async Task SaveToFileAsync(StoreConfiguration configuration)
	{
		try
		{
			await _jsonUtility.SaveToFileAsync(configuration, _storeConfigPath);
		}
		catch (Exception ex)
		{
			_logger.LogWarning(ex, "Failed to save User Configuration to file.");
			throw;
		}
	}

	private void SaveToFile(StoreConfiguration configuration)
	{
		try
		{
			_jsonUtility.SaveToFile(configuration, _storeConfigPath);
		}
		catch (Exception ex)
		{
			_logger.LogWarning(ex, "Failed to save User Configuration to file.");
			throw;
		}
	}

	private async Task<StoreConfiguration> GetFromFileAsync()
	{
		try
		{
			if (File.Exists(_storeConfigPath))
				return await _jsonUtility.ReadFromFileAsync<StoreConfiguration>(_storeConfigPath);

			var configuration = CreateNewUserConfiguration();

			await SaveToFileAsync(configuration);

			return await _jsonUtility.ReadFromFileAsync<StoreConfiguration>(_storeConfigPath);
		}
		catch (Exception ex)
		{
			var errorMessage = $"Failed to get User Configuration from file. {ex.Message}";
			_logger.LogWarning(ex, errorMessage);
			throw;
		}
	}

	private StoreConfiguration GetFromFile()
	{
		try
		{
			if (File.Exists(_storeConfigPath))
				return _jsonUtility.ReadFromFile<StoreConfiguration>(_storeConfigPath);

			var configuration = CreateNewUserConfiguration();

			SaveToFile(configuration);

			return _jsonUtility.ReadFromFile<StoreConfiguration>(_storeConfigPath);
		}
		catch (Exception ex)
		{
			var errorMessage = $"Failed to get User Configuration from file. {ex.Message}";
			_logger.LogWarning(ex, errorMessage);
			throw;
		}
	}

	private static StoreConfiguration CreateNewUserConfiguration()
	{
		return new StoreConfiguration
		{
			StoreFullName = "รุ่งรัศมิ์",
			StoreName = "รุ่งรัศมิ์",
			StoreAddressLine1 = "134 หมู่ 4 ต.คำชะอี อ.คำชะอี",
			StoreAddressLine2 = "จ.มุกดาหาร 49110",
			StorePhoneNumber = "084-602-9150",
			PrinterName = "XP-58",
			BarcodeScannerPortName = "COM4"
		};
	}
}