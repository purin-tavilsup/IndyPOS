using IndyPOS.Domain.Entities.Core;

namespace IndyPOS.Application.UseCases.StoreHub.Products;

/// <summary>
/// Extension methods for mapping Product entities to DTOs.
/// </summary>
public static class ProductExtensions
{
    public static ProductDto ToDto(this Product product)
    {
        return new ProductDto(
            Id: product.Id,
            Barcode: product.Barcode,
            Name: product.Name,
            Description: product.Description,
            Category: product.Category,
            Brand: product.Brand,
            Manufacturer: product.Manufacturer,
            UnitPrice: product.UnitPrice,
            GroupPrice: product.GroupPrice,
            GroupPriceQuantity: product.GroupPriceQuantity,
            IsActive: product.IsActive);
    }

    public static IEnumerable<ProductDto> ToDtos(this IEnumerable<Product> products)
    {
        return products.Select(p => p.ToDto());
    }
}
