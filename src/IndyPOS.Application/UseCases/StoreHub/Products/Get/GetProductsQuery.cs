using Nokpirab;

namespace IndyPOS.Application.UseCases.StoreHub.Products.Get;

/// <summary>
/// Query to retrieve products from StoreHub.
/// </summary>
/// <param name="ActiveOnly">If true, only return active products.</param>
/// <param name="Category">Optional category filter.</param>
/// <param name="SearchTerm">Optional search term for barcode or name.</param>
public record GetProductsQuery(
    bool ActiveOnly = true,
    string? Category = null,
    string? SearchTerm = null) : IQuery<IReadOnlyList<ProductDto>>;
