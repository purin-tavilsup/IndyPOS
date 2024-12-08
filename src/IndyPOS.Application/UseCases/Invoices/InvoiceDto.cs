namespace IndyPOS.Application.UseCases.Invoices;

public record InvoiceDto(
	int InvoiceId,
	decimal Total,
	int? CustomerId,
	int UserId,
	string DateCreated);