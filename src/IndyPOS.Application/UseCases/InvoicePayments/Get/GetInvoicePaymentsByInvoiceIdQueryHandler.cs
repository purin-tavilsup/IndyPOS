using IndyPOS.Application.Abstractions.Pos.Repositories;
using Nokpirab;

namespace IndyPOS.Application.UseCases.InvoicePayments.Get;

public class GetInvoicePaymentsByInvoiceIdQueryHandler : IQueryHandler<GetInvoicePaymentsByInvoiceIdQuery, IEnumerable<InvoicePaymentDto>>
{
	private readonly IInvoicePaymentRepository _invoicePaymentRepository;

	public GetInvoicePaymentsByInvoiceIdQueryHandler(IInvoicePaymentRepository invoicePaymentRepository)
	{
		_invoicePaymentRepository = invoicePaymentRepository;
	}

	public Task<IEnumerable<InvoicePaymentDto>> HandleAsync(GetInvoicePaymentsByInvoiceIdQuery query, CancellationToken cancellationToken = default)
	{
		var results = _invoicePaymentRepository.GetByInvoiceId(query.InvoiceId);

		return Task.FromResult(results.Select(x => x.ToDto()));
	}
}