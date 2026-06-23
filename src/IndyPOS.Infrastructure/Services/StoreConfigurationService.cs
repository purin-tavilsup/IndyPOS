using IndyPOS.Application.Common;
using IndyPOS.Application.Common.Interfaces;
using IndyPOS.Application.Common.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace IndyPOS.Infrastructure.Services;

public class StoreConfigurationService : IStoreConfigurationService
{
	private readonly IJsonService _jsonService;
	private readonly ILogger<StoreConfigurationService> _logger;
	private readonly string _storeConfigPath;

	public StoreConfigurationService(IConfiguration configuration, IJsonService jsonService, ILogger<StoreConfigurationService> logger)
	{
		_jsonService = jsonService;
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

		// Default to the versioned install root (C:\ProgramData\IndyPOS\v4\...).
		// appsettings may still override via Store:ConfigPath if needed.
		return string.IsNullOrWhiteSpace(path) ? InstallPaths.StoreConfigPath : path;
	}

	private async Task SaveToFileAsync(StoreConfiguration configuration)
	{
		try
		{
			await _jsonService.SaveToFileAsync(configuration, _storeConfigPath);
		}
		catch (Exception ex)
		{
			_logger.LogWarning(ex, "Failed to save User Configuration to file");
			throw;
		}
	}

	private void SaveToFile(StoreConfiguration configuration)
	{
		try
		{
			_jsonService.SaveToFile(configuration, _storeConfigPath);
		}
		catch (Exception ex)
		{
			_logger.LogWarning(ex, "Failed to save User Configuration to file");
			throw;
		}
	}

	private async Task<StoreConfiguration> GetFromFileAsync()
	{
		try
		{
			if (File.Exists(_storeConfigPath))
				return await _jsonService.ReadFromFileAsync<StoreConfiguration>(_storeConfigPath);

			var configuration = CreateNewUserConfiguration();
			await TryPersistDefaultAsync(configuration);
			return configuration;
		}
		catch (Exception ex)
		{
			// Store configuration must never be fatal: a POS terminal should still
			// reach the login screen even if the config is unreadable. Fall back to
			// in-memory defaults; the cashier can correct them in Settings.
			_logger.LogWarning(ex, "Failed to read store configuration from '{Path}'. Falling back to defaults.", _storeConfigPath);
			return CreateNewUserConfiguration();
		}
	}

	private StoreConfiguration GetFromFile()
	{
		try
		{
			if (File.Exists(_storeConfigPath))
				return _jsonService.ReadFromFile<StoreConfiguration>(_storeConfigPath);

			var configuration = CreateNewUserConfiguration();
			TryPersistDefault(configuration);
			return configuration;
		}
		catch (Exception ex)
		{
			_logger.LogWarning(ex, "Failed to read store configuration from '{Path}'. Falling back to defaults.", _storeConfigPath);
			return CreateNewUserConfiguration();
		}
	}

	// Best-effort: seed the config file on a fresh machine, but never let a write
	// failure (e.g. a locked-down install dir) crash startup — defaults still work.
	private async Task TryPersistDefaultAsync(StoreConfiguration configuration)
	{
		try
		{
			await _jsonService.SaveToFileAsync(configuration, _storeConfigPath);
		}
		catch (Exception ex)
		{
			_logger.LogWarning(ex, "Failed to persist default store configuration to '{Path}'. Using in-memory defaults.", _storeConfigPath);
		}
	}

	private void TryPersistDefault(StoreConfiguration configuration)
	{
		try
		{
			_jsonService.SaveToFile(configuration, _storeConfigPath);
		}
		catch (Exception ex)
		{
			_logger.LogWarning(ex, "Failed to persist default store configuration to '{Path}'. Using in-memory defaults.", _storeConfigPath);
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
			BarcodeScannerDeviceName = string.Empty,
			SerialPortName = "COM1",
			Code = 1
		};
	}
}