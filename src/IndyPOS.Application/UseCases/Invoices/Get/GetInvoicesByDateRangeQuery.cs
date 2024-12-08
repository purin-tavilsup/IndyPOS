using IndyPOS.Application.Abstractions.Messaging;

namespace IndyPOS.Application.UseCases.Invoices.Get;

public record GetInvoicesByDateRangeQuery(DateOnly StartDate, DateOnly EndDate) : IQuery<IEnumerable<InvoiceDto>>;