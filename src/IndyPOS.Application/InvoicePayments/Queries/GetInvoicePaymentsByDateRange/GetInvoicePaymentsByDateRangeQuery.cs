using IndyPOS.Application.Abstractions.Messaging;

namespace IndyPOS.Application.InvoicePayments.Queries.GetInvoicePaymentsByDateRange;

public record GetInvoicePaymentsByDateRangeQuery(DateOnly StartDate, DateOnly EndDate) : IQuery<IEnumerable<InvoicePaymentDto>>;