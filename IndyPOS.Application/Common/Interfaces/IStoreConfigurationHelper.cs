using IndyPOS.Application.Common.Models;

namespace IndyPOS.Application.Common.Interfaces;

public interface IStoreConfigurationHelper
{
	Task<StoreConfiguration> GetAsync();

	StoreConfiguration Get();

	Task UpdateAsync(StoreConfiguration configuration);
}