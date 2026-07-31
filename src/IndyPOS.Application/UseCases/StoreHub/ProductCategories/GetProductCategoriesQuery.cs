using Nokpirab;

namespace IndyPOS.Application.UseCases.StoreHub.ProductCategories;

/// <summary>Every category for this store, enabled or not, in display order.</summary>
public record GetProductCategoriesQuery : IQuery<IReadOnlyList<ProductCategoryDto>>;
