using IndyPOS.Domain.Entities.Core;

namespace IndyPOS.Application.Abstractions.StoreHub.Repositories;

/// <summary>
/// This store's product-category catalogue. There is deliberately no UpdateAsync: the spec rules
/// out an admin editing screen, because categories are stable — unlike the government payment
/// campaigns that drove the payment-method catalogue. Add it when there is a reason.
/// </summary>
public interface IProductCategoryRepository
{
    Task<IReadOnlyList<ProductCategory>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<ProductCategory?> GetByCodeAsync(string code, CancellationToken cancellationToken = default);
    Task AddAsync(ProductCategory category, CancellationToken cancellationToken = default);
}
