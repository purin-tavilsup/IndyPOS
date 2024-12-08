using IndyPOS.Application.Abstractions.Messaging;

namespace IndyPOS.Application.UseCases.PayLaterPayments.Get;

public record GetPayLaterPaymentByIdQuery(int Id) : IQuery<PayLaterPaymentDto>;