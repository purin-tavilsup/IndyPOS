using IndyPOS.Domain.Entities.Core;

namespace IndyPOS.Application.Abstractions.StoreHub.Repositories;

/// <summary>
/// Repository for store settings (key-value store).
/// </summary>
public interface IStoreSettingRepository
{
    /// <summary>
    /// Gets a setting value by key. Returns null if not found.
    /// </summary>
    Task<string?> GetValueAsync(string key, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sets a setting value. Creates if not exists, updates if exists.
    /// </summary>
    Task SetValueAsync(string key, string value, CancellationToken cancellationToken = default);

    /// <summary>
    /// Atomically increments a numeric setting and returns the new value.
    /// Creates with value 1 if not exists.
    /// </summary>
    Task<int> IncrementAsync(string key, CancellationToken cancellationToken = default);
}
