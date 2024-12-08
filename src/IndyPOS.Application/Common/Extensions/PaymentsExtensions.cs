using IndyPOS.Application.Common.Models;
using IndyPOS.Application.Common.Enums;
using IndyPOS.Application.UseCases.InvoicePayments;

namespace IndyPOS.Application.Common.Extensions;

public static class PaymentsExtensions
{
	public static bool HasPayLayerPayment(this IEnumerable<Payment> payments)
	{
		return payments.Any(x => x.PaymentTypeId == (int)PaymentType.PayLater);
	}

	public static bool HasPayLayerPayment(this IEnumerable<InvoicePaymentDto> payments)
	{
		return payments.Any(x => x.PaymentTypeId == (int)PaymentType.PayLater);
	}

	public static Payment ToPayment(this InvoicePaymentDto payment)
	{
		return new Payment
		{
			Amount = payment.Amount,
			Note = payment.Note,
			PaymentTypeId = payment.PaymentTypeId
		};
	}

	public static IEnumerable<Payment> ToPayments(this IEnumerable<InvoicePaymentDto> payments)
	{
		return payments.Select(x => x.ToPayment());
	}
}