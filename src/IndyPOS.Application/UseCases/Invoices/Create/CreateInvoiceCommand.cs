using IndyPOS.Application.Abstractions.Messaging;

namespace IndyPOS.Application.UseCases.Invoices.Create;

public record CreateInvoiceCommand : ICommand<int>
{
	public decimal Total { get; set; }

	public int? CustomerId { get; set; }

	public int UserId { get; set; }
}