using IndyPOS.Application.Abstractions.Messaging;

namespace IndyPOS.Application.PayLaterPayments.Queries.GetPayLaterPaymentById;

public record GetPayLaterPaymentByIdQuery(int Id) : IQuery<PayLaterPaymentDto>;