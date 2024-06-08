using IndyPOS.Application.InventoryProducts.Commands.Create;
using IndyPOS.Application.InventoryProducts.Commands.Update;
using IndyPOS.Domain.Entities;

namespace IndyPOS.Application.InventoryProducts;

internal static class InventoryProductExtensions
{
	internal static InventoryProductDto ToDto(this InventoryProduct entity)
	{
		var dto = new InventoryProductDto(entity.InventoryProductId,
										  entity.Barcode,
										  entity.Description,
										  entity.Manufacturer,
										  entity.Brand,
										  entity.Category,
										  entity.UnitPrice,
										  entity.QuantityInStock,
										  entity.GroupPrice,
										  entity.GroupPriceQuantity,
										  entity.IsTrackable,
										  entity.DateCreated,
										  entity.DateUpdated);
        return dto;
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