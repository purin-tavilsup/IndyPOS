using IndyPOS.Application.Abstractions.Pos.Repositories;
using Nokpirab;

namespace IndyPOS.Application.UseCases.PayLaterPayments.Get;

public class GetPayLaterPaymentsQueryHandler : IQueryHandler<GetPayLaterPaymentsQuery, IEnumerable<PayLaterPaymentDto>>
{
	private readonly IPayLaterPaymentRepository _paymentRepository;

	public GetPayLaterPaymentsQueryHandler(IPayLaterPaymentRepository paymentRepository)
	{
		_paymentRepository = paymentRepository;
	}

	public Task<IEnumerable<PayLaterPaymentDto>> HandleAsync(GetPayLaterPaymentsQuery query, CancellationToken cancellationToken = default)
	{
		var results = _paymentRepository.GetAll();

		return Task.FromResult(results.Select(x => x.ToDto()));
	}
}