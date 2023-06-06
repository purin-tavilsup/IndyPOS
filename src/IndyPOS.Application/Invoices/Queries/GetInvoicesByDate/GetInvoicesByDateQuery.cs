using IndyPOS.Application.Abstractions.Messaging;

namespace IndyPOS.Application.Invoices.Queries.GetInvoicesByDate;

public record GetInvoicesByDateQuery(DateOnly Date) : IQuery<IEnumerable<InvoiceDto>>;