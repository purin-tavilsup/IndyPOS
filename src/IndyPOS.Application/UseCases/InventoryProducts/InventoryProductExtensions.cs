using IndyPOS.Application.Common.Helpers;
using IndyPOS.Domain.Entities;

namespace IndyPOS.Application.UseCases.InventoryProducts;

internal static class InventoryProductExtensions
{
	internal static InventoryProductDto ToDto(this InventoryProduct entity)
	{
		return new InventoryProductDto
		{
			Id = LegacyIdHelper.ToGuid(entity.InventoryProductId),
#pragma warning disable CS0618 // Keep for legacy SQLite compatibility
			InventoryProductId = entity.InventoryProductId,
#pragma warning restore CS0618
			Barcode = entity.Barcode,
			Description = entity.Description,
			Manufacturer = entity.Manufacturer,
			Brand = entity.Brand,
			Category = entity.Category,
			UnitPrice = entity.UnitPrice,
			QuantityInStock = entity.QuantityInStock,
			GroupPrice = entity.GroupPrice,
			GroupPriceQuantity = entity.GroupPriceQuantity,
			IsTrackable = entity.IsTrackable,
			DateCreated = entity.DateCreated,
			DateUpdated = entity.DateUpdated
		};
    }
}