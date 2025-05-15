using IndyPOS.Application.Common.Interfaces;
using Nokpirab;

namespace IndyPOS.Application.UseCases.Invoices.Get;

public class GetInvoicesByDateRangeQueryHandler : IQueryHandler<GetInvoicesByDateRangeQuery, IEnumerable<InvoiceDto>>
{
	private readonly IInvoiceRepository _invoiceRepository;

	public GetInvoicesByDateRangeQueryHandler(IInvoiceRepository invoiceRepository)
	{
		_invoiceRepository = invoiceRepository;
	}

	public Task<IEnumerable<InvoiceDto>> HandleAsync(GetInvoicesByDateRangeQuery query, CancellationToken cancellationToken = default)
	{
		var results = _invoiceRepository.GetByDateRange(query.StartDate, query.EndDate);

		return Task.FromResult(results.Select(x => x.ToDto()));
	}
}