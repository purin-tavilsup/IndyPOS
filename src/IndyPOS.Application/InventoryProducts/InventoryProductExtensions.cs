using IndyPOS.Application.InventoryProducts.Commands.CreateInventoryProduct;
using IndyPOS.Application.InventoryProducts.Commands.UpdateInventoryProduct;
using IndyPOS.Domain.Entities;

namespace IndyPOS.Application.InventoryProducts;

internal static class InventoryProductExtensions
{
	internal static InventoryProductDto ToDto(this InventoryProduct entity)
    {
        var dto = new InventoryProductDto
        {
            InventoryProductId = entity.InventoryProductId,
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