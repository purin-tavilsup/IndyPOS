using IndyPOS.Application.Abstractions.Messaging;

namespace IndyPOS.Application.InvoicePayments.Queries.GetInvoicePaymentsByPaymentTypeId;

public record GetInvoicePaymentsByPaymentTypeIdQuery(int PaymentTypeId) : IQuery<IEnumerable<InvoicePaymentDto>>;