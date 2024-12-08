using IndyPOS.Application.Abstractions.Messaging;

namespace IndyPOS.Application.UseCases.InvoicePayments.Get;

public record GetInvoicePaymentsByDateQuery(DateOnly Date) : IQuery<IEnumerable<InvoicePaymentDto>>;