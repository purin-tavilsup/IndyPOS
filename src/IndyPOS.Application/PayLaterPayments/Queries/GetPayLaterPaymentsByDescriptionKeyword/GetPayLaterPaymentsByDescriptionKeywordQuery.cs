using IndyPOS.Application.Abstractions.Messaging;

namespace IndyPOS.Application.PayLaterPayments.Queries.GetPayLaterPaymentsByDescriptionKeyword;

public record GetPayLaterPaymentsByDescriptionKeywordQuery(string Keyword) : IQuery<IEnumerable<PayLaterPaymentDto>>;