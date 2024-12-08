using IndyPOS.Application.Abstractions.Messaging;

namespace IndyPOS.Application.UseCases.PayLaterPayments.Get;

public record GetPayLaterPaymentsByDateRangeQuery(DateOnly StartDate, DateOnly EndDate) : IQuery<IEnumerable<PayLaterPaymentDto>>;