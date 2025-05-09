using IndyPOS.Application.Abstractions.Pos.Repositories;
using Nokpirab;

namespace IndyPOS.Application.UseCases.InvoiceProducts.Get;

public class GetInvoiceProductsByDateQueryHandler : IQueryHandler<GetInvoiceProductsByDateQuery, IEnumerable<InvoiceProductDto>>
{
	private readonly IInvoiceProductRepository _invoiceProductRepository;

	public GetInvoiceProductsByDateQueryHandler(IInvoiceProductRepository invoiceProductRepository)
	{
		_invoiceProductRepository = invoiceProductRepository;
	}

	public Task<IEnumerable<InvoiceProductDto>> HandleAsync(GetInvoiceProductsByDateQuery query, CancellationToken cancellationToken = default)
	{
		var results = _invoiceProductRepository.GetByDate(query.Date);

		return Task.FromResult(results.Select(x => x.ToDto()));
	}
}