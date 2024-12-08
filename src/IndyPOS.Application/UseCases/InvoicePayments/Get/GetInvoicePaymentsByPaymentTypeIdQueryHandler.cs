using IndyPOS.Application.Abstractions.Messaging;
using IndyPOS.Application.Abstractions.Pos.Repositories;

namespace IndyPOS.Application.UseCases.InvoicePayments.Get;

public class GetInvoicePaymentsByPaymentTypeIdQueryHandler : IQueryHandler<GetInvoicePaymentsByPaymentTypeIdQuery, IEnumerable<InvoicePaymentDto>>
{
	private readonly IInvoicePaymentRepository _invoicePaymentRepository;

	public GetInvoicePaymentsByPaymentTypeIdQueryHandler(IInvoicePaymentRepository invoicePaymentRepository)
	{
		_invoicePaymentRepository = invoicePaymentRepository;
	}

	public Task<IEnumerable<InvoicePaymentDto>> Handle(GetInvoicePaymentsByPaymentTypeIdQuery query, CancellationToken cancellationToken)
	{
		var results = _invoicePaymentRepository.GetByPaymentTypeId(query.PaymentTypeId);

		return Task.FromResult(results.Select(x => x.ToDto()));
	}
}