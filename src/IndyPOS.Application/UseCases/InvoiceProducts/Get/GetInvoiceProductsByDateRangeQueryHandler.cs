using IndyPOS.Application.Abstractions.Pos.Repositories;
using Nokpirab;

namespace IndyPOS.Application.UseCases.InvoiceProducts.Get;

public class GetInvoiceProductsByDateRangeQueryHandler : IQueryHandler<GetInvoiceProductsByDateRangeQuery, IEnumerable<InvoiceProductDto>>
{
	private readonly IInvoiceProductRepository _invoiceProductRepository;

	public GetInvoiceProductsByDateRangeQueryHandler(IInvoiceProductRepository invoiceProductRepository)
	{
		_invoiceProductRepository = invoiceProductRepository;
	}

	public Task<IEnumerable<InvoiceProductDto>> HandleAsync(GetInvoiceProductsByDateRangeQuery query, CancellationToken cancellationToken = default)
	{
		var results = _invoiceProductRepository.GetByDateRange(query.StartDate, query.EndDate);

		return Task.FromResult(results.Select(x => x.ToDto()));
	}
}