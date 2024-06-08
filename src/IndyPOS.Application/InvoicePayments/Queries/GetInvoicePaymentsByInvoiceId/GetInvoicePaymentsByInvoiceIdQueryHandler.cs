using IndyPOS.Application.Abstractions.Messaging;
using IndyPOS.Application.Abstractions.Pos.Repositories;

namespace IndyPOS.Application.InvoicePayments.Queries.GetInvoicePaymentsByInvoiceId;

public class GetInvoicePaymentsByInvoiceIdQueryHandler : IQueryHandler<GetInvoicePaymentsByInvoiceIdQuery, IEnumerable<InvoicePaymentDto>>
{
	private readonly IInvoicePaymentRepository _invoicePaymentRepository;

	public GetInvoicePaymentsByInvoiceIdQueryHandler(IInvoicePaymentRepository invoicePaymentRepository)
	{
		_invoicePaymentRepository = invoicePaymentRepository;
	}

	public Task<IEnumerable<InvoicePaymentDto>> Handle(GetInvoicePaymentsByInvoiceIdQuery query, CancellationToken cancellationToken)
	{
		var results = _invoicePaymentRepository.GetByInvoiceId(query.InvoiceId);

		return Task.FromResult(results.Select(x => x.ToDto()));
	}
}