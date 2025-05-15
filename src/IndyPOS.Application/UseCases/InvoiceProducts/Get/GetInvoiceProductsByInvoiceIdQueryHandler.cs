using IndyPOS.Application.Abstractions.Pos.Repositories;
using Nokpirab;

namespace IndyPOS.Application.UseCases.InvoiceProducts.Get;

public class GetInvoiceProductsByInvoiceIdQueryHandler : IQueryHandler<GetInvoiceProductsByInvoiceIdQuery, IEnumerable<InvoiceProductDto>>
{
	private readonly IInvoiceProductRepository _invoiceProductRepository;

	public GetInvoiceProductsByInvoiceIdQueryHandler(IInvoiceProductRepository invoiceProductRepository)
	{
		_invoiceProductRepository = invoiceProductRepository;
	}

	public Task<IEnumerable<InvoiceProductDto>> HandleAsync(GetInvoiceProductsByInvoiceIdQuery query, CancellationToken cancellationToken = default)
	{
		var results = _invoiceProductRepository.GetByInvoiceId(query.InvoiceId);

		return Task.FromResult(results.Select(x => x.ToDto()));
	}
}