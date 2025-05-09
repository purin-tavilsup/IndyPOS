using IndyPOS.Application.Common.Interfaces;
using Nokpirab;

namespace IndyPOS.Application.UseCases.Invoices.Get;

public class GetInvoicesByDateQueryHandler : IQueryHandler<GetInvoicesByDateQuery, IEnumerable<InvoiceDto>>
{
	private readonly IInvoiceRepository _invoiceRepository;

	public GetInvoicesByDateQueryHandler(IInvoiceRepository invoiceRepository)
	{
		_invoiceRepository = invoiceRepository;
	}

	public Task<IEnumerable<InvoiceDto>> HandleAsync(GetInvoicesByDateQuery query, CancellationToken cancellationToken = default)
	{
		var results = _invoiceRepository.GetByDate(query.Date);

		return Task.FromResult(results.Select(x => x.ToDto()));
	}
}