using IndyPOS.Application.Invoices.Commands.CreateInvoice;
using IndyPOS.Domain.Entities;

namespace IndyPOS.Application.Invoices;

internal static class InvoiceExtensions
{
	internal static InvoiceDto ToDto(this Invoice entity)
	{
		var dto = new InvoiceDto(entity.InvoiceId,
								 entity.Total,
								 entity.CustomerId,
								 entity.UserId,
								 entity.DateCreated);
		return dto;
	}

	internal static Invoice ToEntity(this CreateInvoiceCommand command)
	{
		var entity = new Invoice
		{
			Total = command.Total,
			CustomerId = command.CustomerId,
			UserId = command.UserId
		};

		return entity;
	}
}