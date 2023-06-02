using IndyPOS.Application.Abstractions.Messaging;

namespace IndyPOS.Application.Invoices.Queries.GetInvoicesByDateRange;

public record GetInvoicesByDateRangeQuery(DateOnly StartDate, DateOnly EndDate) : IQuery<IEnumerable<InvoiceDto>>;