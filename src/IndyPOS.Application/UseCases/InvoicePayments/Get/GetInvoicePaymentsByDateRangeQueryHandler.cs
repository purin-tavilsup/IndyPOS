using IndyPOS.Application.Abstractions.Pos.Repositories;
using Nokpirab;

namespace IndyPOS.Application.UseCases.InvoicePayments.Get;

public class GetInvoicePaymentsByDateRangeQueryHandler : IQueryHandler<GetInvoicePaymentsByDateRangeQuery, IEnumerable<InvoicePaymentDto>>
{
	private readonly IInvoicePaymentRepository _invoicePaymentRepository;

	public GetInvoicePaymentsByDateRangeQueryHandler(IInvoicePaymentRepository invoicePaymentRepository)
	{
		_invoicePaymentRepository = invoicePaymentRepository;
	}

	public Task<IEnumerable<InvoicePaymentDto>> HandleAsync(GetInvoicePaymentsByDateRangeQuery query, CancellationToken cancellationToken = default)
	{
		var results = _invoicePaymentRepository.GetByDateRange(query.StartDate, query.EndDate);

		return Task.FromResult(results.Select(x => x.ToDto()));
	}
}