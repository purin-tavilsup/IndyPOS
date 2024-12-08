using IndyPOS.Application.Abstractions.Messaging;

namespace IndyPOS.Application.UseCases.Invoices.Get;

public record GetInvoicesByDateQuery(DateOnly Date) : IQuery<IEnumerable<InvoiceDto>>;