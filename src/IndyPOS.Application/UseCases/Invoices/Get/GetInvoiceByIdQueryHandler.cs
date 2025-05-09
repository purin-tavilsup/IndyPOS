using IndyPOS.Application.Common.Interfaces;
using Nokpirab;

namespace IndyPOS.Application.UseCases.Invoices.Get;

public class GetInvoiceByIdQueryHandler : IQueryHandler<GetInvoiceByIdQuery, InvoiceDto>
{
	private readonly IInvoiceRepository _invoiceRepository;

	public GetInvoiceByIdQueryHandler(IInvoiceRepository invoiceRepository)
	{
		_invoiceRepository = invoiceRepository;
	}

	public Task<InvoiceDto> HandleAsync(GetInvoiceByIdQuery query, CancellationToken cancellationToken = default)
	{
		var result = _invoiceRepository.GetById(query.Id);

		return Task.FromResult(result.ToDto());
	}
}