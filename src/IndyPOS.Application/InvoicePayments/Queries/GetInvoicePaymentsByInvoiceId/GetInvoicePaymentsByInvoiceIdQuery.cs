using IndyPOS.Application.Abstractions.Messaging;

namespace IndyPOS.Application.InvoicePayments.Queries.GetInvoicePaymentsByInvoiceId;

public record GetInvoicePaymentsByInvoiceIdQuery(int InvoiceId) : IQuery<IEnumerable<InvoicePaymentDto>>;