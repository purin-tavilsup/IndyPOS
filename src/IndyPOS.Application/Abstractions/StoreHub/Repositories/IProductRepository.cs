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
}
