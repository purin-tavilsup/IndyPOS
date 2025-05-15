using IndyPOS.Application.Abstractions.Pos.Repositories;
using Nokpirab;

namespace IndyPOS.Application.UseCases.InvoicePayments.Get;

public class GetInvoicePaymentsByPaymentTypeIdQueryHandler : IQueryHandler<GetInvoicePaymentsByPaymentTypeIdQuery, IEnumerable<InvoicePaymentDto>>
{
	private readonly IInvoicePaymentRepository _invoicePaymentRepository;

	public GetInvoicePaymentsByPaymentTypeIdQueryHandler(IInvoicePaymentRepository invoicePaymentRepository)
	{
		_invoicePaymentRepository = invoicePaymentRepository;
	}

	public Task<IEnumerable<InvoicePaymentDto>> HandleAsync(GetInvoicePaymentsByPaymentTypeIdQuery query, CancellationToken cancellationToken = default)
	{
		var results = _invoicePaymentRepository.GetByPaymentTypeId(query.PaymentTypeId);

		return Task.FromResult(results.Select(x => x.ToDto()));
	}
}