using IndyPOS.Domain.Entities.Core;

namespace IndyPOS.Application.Abstractions.StoreHub.Repositories;

/// <summary>
/// Repository interface for StoreHub Product operations.
/// </summary>
public interface IProductRepository
{
    Task<IReadOnlyList<Product>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Product>> GetActiveAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Product>> GetByCategoryAsync(string category, bool activeOnly = true, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Product>> SearchAsync(string searchTerm, bool activeOnly = true, CancellationToken cancellationToken = default);

    Task<Product?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<Product?> GetByBarcodeAsync(string barcode, CancellationToken cancellationToken = default);

    Task AddAsync(Product product, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing product.
    /// </summary>
    Task UpdateAsync(Product product, CancellationToken cancellationToken = default);

    /// <summary>
    /// Soft deletes a product by setting IsActive = false.
    /// </summary>
    Task SoftDeleteAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a barcode already exists.
    /// Optionally excludes a specific product ID (useful for updates).
    /// </summary>
    Task<bool> ExistsByBarcodeAsync(string barcode, Guid? excludeId = null, CancellationToken cancellationToken = default);
}
