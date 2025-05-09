using Nokpirab;

namespace IndyPOS.Application.UseCases.PayLaterPayments.Get;

public record GetPayLaterPaymentByInvoiceIdQuery(int InvoiceId) : IQuery<PayLaterPaymentDto?>;