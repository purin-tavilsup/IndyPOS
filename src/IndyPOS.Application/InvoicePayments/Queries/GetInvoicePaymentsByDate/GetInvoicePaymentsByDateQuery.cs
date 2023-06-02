using IndyPOS.Application.Abstractions.Messaging;

namespace IndyPOS.Application.InvoicePayments.Queries.GetInvoicePaymentsByDate;

public record GetInvoicePaymentsByDateQuery(DateOnly Date) : IQuery<IEnumerable<InvoicePaymentDto>>;