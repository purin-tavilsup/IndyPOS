using IndyPOS.Application.Abstractions.Messaging;

namespace IndyPOS.Application.PayLaterPayments.Queries.GetPayLaterPayments;

public record GetPayLaterPaymentsQuery() : IQuery<IEnumerable<PayLaterPaymentDto>>;