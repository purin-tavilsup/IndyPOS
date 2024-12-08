using IndyPOS.Application.Abstractions.Messaging;
using IndyPOS.Application.Abstractions.Pos.Repositories;

namespace IndyPOS.Application.UseCases.PayLaterPayments.Get;

public class GetPayLaterPaymentByInvoiceIdQueryHandler : IQueryHandler<GetPayLaterPaymentByInvoiceIdQuery, PayLaterPaymentDto?>
{
	private readonly IPayLaterPaymentRepository _paymentRepository;

	public GetPayLaterPaymentByInvoiceIdQueryHandler(IPayLaterPaymentRepository paymentRepository)
	{
		_paymentRepository = paymentRepository;
	}

	public Task<PayLaterPaymentDto?> Handle(GetPayLaterPaymentByInvoiceIdQuery query, CancellationToken cancellationToken)
	{
		var result = _paymentRepository.GetPayLaterPaymentByInvoiceId(query.InvoiceId);

		return Task.FromResult(result?.ToDto());
	}
}