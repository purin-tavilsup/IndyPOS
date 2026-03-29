using IndyPOS.Application.Common.Helpers;
using IndyPOS.Application.Common.Models;
using IndyPOS.Application.UseCases.InvoiceProducts;

namespace IndyPOS.Application.Common.Extensions;

public static class ProductExtensions
{
    public static bool HasGroupPrice(this Product product)
    {
        if (product.GroupPriceQuantity is null)
        {
            return false;
        }

        return product.GroupPriceQuantity >= 1;
    }

	public static Product ToProduct(this InvoiceProductDto product)
	{
		return new Product
		{
			Id = LegacyIdHelper.ToGuid(product.InventoryProductId),
#pragma warning disable CS0618 // Keep for legacy SQLite compatibility
			InventoryProductId = product.InventoryProductId,
#pragma warning restore CS0618
			Barcode = product.Barcode,
			Description = product.Description,
			Manufacturer = product.Manufacturer,
			Brand = product.Brand,
			Category = product.Category,
			UnitPrice = product.UnitPrice,
			OriginalUnitPrice = product.UnitPrice,
			Quantity = product.Quantity,
			GroupPrice = product.GroupPrice,
			IsGroupProduct = product.IsGroupProduct
		};
	}

	public static IEnumerable<Product> ToProducts(this IEnumerable<InvoiceProductDto> products)
	{
		return products.Select(x => x.ToProduct());
	}
}