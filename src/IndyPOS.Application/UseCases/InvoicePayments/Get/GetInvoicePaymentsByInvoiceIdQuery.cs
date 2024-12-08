using IndyPOS.Application.Abstractions.Messaging;

namespace IndyPOS.Application.UseCases.InvoicePayments.Get;

public record GetInvoicePaymentsByInvoiceIdQuery(int InvoiceId) : IQuery<IEnumerable<InvoicePaymentDto>>;