using IndyPOS.Application.Abstractions.Pos.Repositories;
using Nokpirab;

namespace IndyPOS.Application.UseCases.PayLaterPayments.Get;

public class GetPayLaterPaymentsByDateRangeQueryHandler : IQueryHandler<GetPayLaterPaymentsByDateRangeQuery, IEnumerable<PayLaterPaymentDto>>
{
	private readonly IPayLaterPaymentRepository _paymentRepository;

	public GetPayLaterPaymentsByDateRangeQueryHandler(IPayLaterPaymentRepository paymentRepository)
	{
		_paymentRepository = paymentRepository;
	}
	public Task<IEnumerable<PayLaterPaymentDto>> HandleAsync(GetPayLaterPaymentsByDateRangeQuery query, CancellationToken cancellationToken = default)
	{
		var results = _paymentRepository.GetPayLaterPaymentsByDateRange(query.StartDate, query.EndDate);

		return Task.FromResult(results.Select(x => x.ToDto()));
	}
}