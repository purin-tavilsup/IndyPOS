using IndyPOS.Application.Abstractions.Messaging;

namespace IndyPOS.Application.Invoices.Queries.GetInvoiceById;

public record GetInvoiceByIdQuery(int Id) : IQuery<InvoiceDto>;