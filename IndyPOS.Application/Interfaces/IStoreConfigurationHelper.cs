using IndyPOS.Application.Models;

namespace IndyPOS.Application.Interfaces;

public interface IStoreConfigurationHelper
{
	Task<StoreConfiguration> GetAsync();

	StoreConfiguration Get();

	Task UpdateAsync(StoreConfiguration configuration);
}