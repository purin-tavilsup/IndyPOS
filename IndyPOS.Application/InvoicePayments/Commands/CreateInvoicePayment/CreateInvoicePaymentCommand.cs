using IndyPOS.Application.Abstractions.Messaging;

namespace IndyPOS.Application.InvoicePayments.Commands.CreateInvoicePayment;

public record CreateInvoicePaymentCommand : ICommand
{
	public int InvoiceId { get; set; }

	public int PaymentTypeId { get; set; }

	public decimal Amount { get; set; }

	public string Note { get; set; } = string.Empty;
}