using IndyPOS.Application.Abstractions.StoreHub.Repositories;
using Nokpirab;

namespace IndyPOS.Application.UseCases.StoreHub.ProductCategories;

public class GetProductCategoriesQueryHandler
    : IQueryHandler<GetProductCategoriesQuery, IReadOnlyList<ProductCategoryDto>>
{
    private readonly IProductCategoryRepository _repository;

    public GetProductCategoriesQueryHandler(IProductCategoryRepository repository)
    {
        _repository = repository;
    }

    public async Task<IReadOnlyList<ProductCategoryDto>> HandleAsync(
        GetProductCategoriesQuery query, CancellationToken cancellationToken = default)
    {
        var categories = await _repository.GetAllAsync(cancellationToken);

        return categories
            .Select(c => new ProductCategoryDto(c.Code, c.DisplayName, c.Kind, c.IsEnabled, c.DisplayOrder))
            .ToList();
    }
}
