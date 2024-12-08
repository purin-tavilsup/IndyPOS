using IndyPOS.Application.Abstractions.Messaging;

namespace IndyPOS.Application.UseCases.PayLaterPayments.Get;

public record GetPayLaterPaymentsQuery() : IQuery<IEnumerable<PayLaterPaymentDto>>;