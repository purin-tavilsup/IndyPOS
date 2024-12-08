using IndyPOS.Application.Abstractions.Messaging;

namespace IndyPOS.Application.UseCases.InvoicePayments.Create;

public record CreateInvoicePaymentCommand : ICommand<int>
{
	public int InvoiceId { get; set; }

	public int PaymentTypeId { get; set; }

	public decimal Amount { get; set; }

	public string Note { get; set; } = string.Empty;
}