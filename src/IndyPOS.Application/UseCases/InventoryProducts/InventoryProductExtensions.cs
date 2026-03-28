using IndyPOS.Application.UseCases.InventoryProducts.Create;
using IndyPOS.Application.UseCases.InventoryProducts.Update;
using IndyPOS.Domain.Entities;

namespace IndyPOS.Application.UseCases.InventoryProducts;

internal static class InventoryProductExtensions
{
	internal static InventoryProductDto ToDto(this InventoryProduct entity)
	{
		return new InventoryProductDto
		{
			InventoryProductId = entity.InventoryProductId,
			StoreHubProductId = null, // SQLite entities don't have StoreHub IDs
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

	internal static InventoryProduct ToEntity(this CreateInventoryProductCommand command)
    {
		var entity = new InventoryProduct
		{
			Barcode = command.Barcode,
			Description = command.Description,
			Manufacturer = command.Manufacturer,
			Brand = command.Brand,
			Category = command.Category,
			UnitPrice = command.UnitPrice,
			QuantityInStock = command.QuantityInStock,
			GroupPrice = command.GroupPrice,
			GroupPriceQuantity = command.GroupPriceQuantity,
			IsTrackable = command.IsTrackable
		};

        return entity;
    }

	internal static InventoryProduct ToEntity(this UpdateInventoryProductCommand command)
    {
		var entity = new InventoryProduct
		{
			InventoryProductId = command.Id,
			Description = command.Description,
			Manufacturer = command.Manufacturer,
			Brand = command.Brand,
			Category = command.Category,
			UnitPrice = command.UnitPrice,
			QuantityInStock = command.QuantityInStock,
			GroupPrice = command.GroupPrice,
			GroupPriceQuantity = command.GroupPriceQuantity
		};

        return entity;
    }
}