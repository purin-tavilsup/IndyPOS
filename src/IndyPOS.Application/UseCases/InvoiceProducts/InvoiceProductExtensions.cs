using IndyPOS.Application.UseCases.InvoiceProducts.Create;
using IndyPOS.Domain.Entities;

namespace IndyPOS.Application.UseCases.InvoiceProducts;

internal static class InvoiceProductExtensions
{
	internal static InvoiceProductDto ToDto(this InvoiceProduct entity)
	{
		var dto = new InvoiceProductDto(entity.InvoiceProductId,
										entity.Priority,
										entity.InvoiceId,
										entity.InventoryProductId,
										entity.Barcode,
										entity.Description,
										entity.Manufacturer,
										entity.Brand,
										entity.Category,
										entity.UnitPrice,
										entity.Quantity,
										entity.DateCreated,
										entity.Note,
										entity.GroupPrice,
										entity.IsGroupProduct);
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