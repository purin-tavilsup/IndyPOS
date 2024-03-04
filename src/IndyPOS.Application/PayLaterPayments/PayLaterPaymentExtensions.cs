using IndyPOS.Application.PayLaterPayments.Commands.CreatePayLaterPayment;
using IndyPOS.Application.PayLaterPayments.Commands.UpdatePayLaterPayment;
using IndyPOS.Domain.Entities;

namespace IndyPOS.Application.PayLaterPayments;

internal static class PayLaterPaymentExtensions
{
	internal static PayLaterPaymentDto ToDto(this PayLaterPayment entity)
	{
		var dto = new PayLaterPaymentDto
		{
			PaymentId = entity.PaymentId,
			Description = entity.Description,
			InvoiceId = entity.InvoiceId,
			ReceivableAmount = entity.PayLaterAmount,
			PaidAmount = entity.PaidAmount,
			IsCompleted = entity.IsCompleted,
			DateCreated = entity.DateCreated,
			DateUpdated = entity.DateUpdated
		};

		return dto;
	}

	internal static PayLaterPayment ToEntity(this CreatePayLaterPaymentCommand command)
	{
		var entity = new PayLaterPayment
		{
			PaymentId = command.PaymentId,
			Description = command.Description,
			InvoiceId = command.InvoiceId,
			PayLaterAmount = command.ReceivableAmount
		};

		return entity;
	}

	internal static PayLaterPayment ToEntity(this UpdatePayLaterPaymentCommand command)
	{
		var entity = new PayLaterPayment
		{
			PaymentId = command.PaymentId,
			PaidAmount = command.PaidAmount,
			IsCompleted = command.IsCompleted
		};

		return entity;
	}
}