using IndyPOS.Application.InvoiceProducts.Commands.CreateInvoiceProduct;
using IndyPOS.Domain.Entities;

namespace IndyPOS.Application.InvoiceProducts;

internal static class InvoiceProductExtensions
{
	internal static InvoiceProductDto ToDto(this InvoiceProduct entity)
	{
		var dto = new InvoiceProductDto
		{
			InvoiceProductId = entity.InvoiceProductId,
			Priority = entity.Priority,
			InvoiceId = entity.InvoiceId,
			InventoryProductId = entity.InventoryProductId,
			Barcode = entity.Barcode,
			Description = entity.Description,
			Manufacturer = entity.Manufacturer,
			Brand = entity.Brand,
			Category = entity.Category,
			UnitPrice = entity.UnitPrice,
			Quantity = entity.Quantity,
			DateCreated = entity.DateCreated,
			Note = entity.Note,
			GroupPrice = entity.GroupPrice,
			IsGroupProduct = entity.IsGroupProduct
		};

		return dto;
	}

	internal static InvoiceProduct ToEntity(this CreateInvoiceProductCommand command)
	{
		var entity = new InvoiceProduct
		{
			InvoiceId = command.InvoiceId,
			Priority = command.Priority,
			InventoryProductId = command.InventoryProductId,
			Barcode = command.Barcode,
			Description = command.Description,
			Manufacturer = command.Manufacturer,
			Brand = command.Brand,
			Category = command.Category,
			UnitPrice = command.UnitPrice,
			Quantity = command.Quantity,
			Note = command.Note,
			GroupPrice = command.GroupPrice,
			IsGroupProduct = command.IsGroupProduct
		};

		return entity;
	}
}