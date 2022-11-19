using IndyPOS.Facade.Models;

namespace IndyPOS.Facade.Interfaces;

public interface IStoreConfigurationHelper
{
	Task<StoreConfiguration> GetAsync();

	StoreConfiguration Get();

	Task UpdateAsync(StoreConfiguration configuration);
}