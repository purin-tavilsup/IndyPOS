using IndyPOS.Application.Abstractions.Messaging;

namespace IndyPOS.Application.PayLaterPayments.Queries.GetPayLaterPaymentsByDateRange;

public record GetPayLaterPaymentsByDateRangeQuery(DateOnly StartDate, DateOnly EndDate) : IQuery<IEnumerable<PayLaterPaymentDto>>;