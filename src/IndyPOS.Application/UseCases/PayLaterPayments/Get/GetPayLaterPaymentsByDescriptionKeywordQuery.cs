using IndyPOS.Application.Abstractions.Messaging;

namespace IndyPOS.Application.UseCases.PayLaterPayments.Get;

public record GetPayLaterPaymentsByDescriptionKeywordQuery(string Keyword) : IQuery<IEnumerable<PayLaterPaymentDto>>;