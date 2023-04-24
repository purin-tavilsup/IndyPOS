using IndyPOS.Application.Abstractions.Messaging;

namespace IndyPOS.Application.Invoices.Commands.CreateInvoice;

public record CreateInvoiceCommand : ICommand
{
	public decimal Total { get; set; }

	public int? CustomerId { get; set; }

	public int UserId { get; set; }
}