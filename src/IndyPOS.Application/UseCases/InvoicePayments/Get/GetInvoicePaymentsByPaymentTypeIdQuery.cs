using IndyPOS.Application.Abstractions.Messaging;

namespace IndyPOS.Application.UseCases.InvoicePayments.Get;

public record GetInvoicePaymentsByPaymentTypeIdQuery(int PaymentTypeId) : IQuery<IEnumerable<InvoicePaymentDto>>;