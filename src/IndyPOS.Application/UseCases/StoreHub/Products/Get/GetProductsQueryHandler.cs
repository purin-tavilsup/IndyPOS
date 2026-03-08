using IndyPOS.Application.Abstractions.StoreHub.Repositories;
using Nokpirab;

namespace IndyPOS.Application.UseCases.StoreHub.Products.Get;

public class GetProductsQueryHandler : IQueryHandler<GetProductsQuery, IReadOnlyList<ProductDto>>
{
    private readonly IProductRepository _productRepository;

    public GetProductsQueryHandler(IProductRepository productRepository)
    {
        _productRepository = productRepository;
    }

    public async Task<IReadOnlyList<ProductDto>> HandleAsync(
        GetProductsQuery query,
        CancellationToken cancellationToken = default)
    {
        var products = await GetProductsAsync(query, cancellationToken);
        return products.ToDtos().ToList();
    }

    private async Task<IReadOnlyList<Domain.Entities.Core.Product>> GetProductsAsync(
        GetProductsQuery query,
        CancellationToken cancellationToken)
    {
        // Search takes priority
        if (!string.IsNullOrWhiteSpace(query.SearchTerm))
        {
            return await _productRepository.SearchAsync(query.SearchTerm, query.ActiveOnly, cancellationToken);
        }

        // Category filter
        if (!string.IsNullOrWhiteSpace(query.Category))
        {
            return await _productRepository.GetByCategoryAsync(query.Category, query.ActiveOnly, cancellationToken);
        }

        // Default: all or active only
        return query.ActiveOnly
            ? await _productRepository.GetActiveAsync(cancellationToken)
            : await _productRepository.GetAllAsync(cancellationToken);
    }
}
