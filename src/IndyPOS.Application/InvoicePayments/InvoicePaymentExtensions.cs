using IndyPOS.Application.InvoicePayments.Commands.CreateInvoicePayment;
using IndyPOS.Domain.Entities;

namespace IndyPOS.Application.InvoicePayments;

internal static class InvoicePaymentExtensions
{
	internal static InvoicePaymentDto ToDto(this Payment entity)
	{
		var dto = new InvoicePaymentDto
		{
			PaymentId = entity.PaymentId,
			InvoiceId = entity.InvoiceId,
			PaymentTypeId = entity.PaymentTypeId,
			Amount = entity.Amount,
			DateCreated = entity.DateCreated,
			Note = entity.Note
		};

		return dto;
	}

	internal static Payment ToEntity(this CreateInvoicePaymentCommand command)
	{
		var entity = new Payment
		{
			InvoiceId = command.InvoiceId,
			PaymentTypeId = command.PaymentTypeId,
			Amount = command.Amount,
			Note = command.Note
		};

		return entity;
	}
}