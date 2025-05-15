using IndyPOS.Application.Abstractions.Pos.Repositories;
using Nokpirab;

namespace IndyPOS.Application.UseCases.PayLaterPayments.Get;

public class GetPayLaterPaymentByIdQueryHandler : IQueryHandler<GetPayLaterPaymentByIdQuery, PayLaterPaymentDto>
{
	private readonly IPayLaterPaymentRepository _paymentRepository;

	public GetPayLaterPaymentByIdQueryHandler(IPayLaterPaymentRepository paymentRepository)
	{
		_paymentRepository = paymentRepository;
	}

	public Task<PayLaterPaymentDto> HandleAsync(GetPayLaterPaymentByIdQuery query, CancellationToken cancellationToken = default)
	{
		var result = _paymentRepository.GetById(query.Id);

		return Task.FromResult(result.ToDto());
	}
}