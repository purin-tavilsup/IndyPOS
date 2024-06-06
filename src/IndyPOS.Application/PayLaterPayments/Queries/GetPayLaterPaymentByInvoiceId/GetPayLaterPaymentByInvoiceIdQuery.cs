using IndyPOS.Application.Abstractions.Messaging;

namespace IndyPOS.Application.PayLaterPayments.Queries.GetPayLaterPaymentByInvoiceId;

public record GetPayLaterPaymentByInvoiceIdQuery(int InvoiceId) : IQuery<PayLaterPaymentDto?>;