using Nokpirab;

namespace IndyPOS.Application.UseCases.PayLaterPayments.Get;

public record GetPayLaterPaymentsQuery : IQuery<IEnumerable<PayLaterPaymentDto>>;