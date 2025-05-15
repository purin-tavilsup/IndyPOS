using IndyPOS.Application.Abstractions.Pos.Repositories;
using Nokpirab;

namespace IndyPOS.Application.UseCases.PayLaterPayments.Get;

public class GetPayLaterPaymentsByDescriptionKeywordQueryHandler : IQueryHandler<GetPayLaterPaymentsByDescriptionKeywordQuery, IEnumerable<PayLaterPaymentDto>>
{
	private readonly IPayLaterPaymentRepository _paymentRepository;

	public GetPayLaterPaymentsByDescriptionKeywordQueryHandler(IPayLaterPaymentRepository paymentRepository)
	{
		_paymentRepository = paymentRepository;
	}

	public Task<IEnumerable<PayLaterPaymentDto>> HandleAsync(GetPayLaterPaymentsByDescriptionKeywordQuery query, CancellationToken cancellationToken = default)
	{
		var keyword = query.Keyword;
		var results = _paymentRepository.GetPayLaterPaymentsByDescriptionKeyword(keyword);

		return Task.FromResult(results.Select(x => x.ToDto()));
	}
}