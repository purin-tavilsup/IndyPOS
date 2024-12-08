using IndyPOS.Application.Abstractions.Messaging;
using IndyPOS.Application.Abstractions.Pos.Repositories;

namespace IndyPOS.Application.UseCases.InvoicePayments.Get;

public class GetInvoicePaymentsByDateQueryHandler : IQueryHandler<GetInvoicePaymentsByDateQuery, IEnumerable<InvoicePaymentDto>>
{
	private readonly IInvoicePaymentRepository _invoicePaymentRepository;

    public GetInvoicePaymentsByDateQueryHandler(IInvoicePaymentRepository invoicePaymentRepository)
    {
        _invoicePaymentRepository = invoicePaymentRepository;
    }

    public Task<IEnumerable<InvoicePaymentDto>> Handle(GetInvoicePaymentsByDateQuery query, CancellationToken cancellationToken)
	{
		var results = _invoicePaymentRepository.GetByDate(query.Date);

        return Task.FromResult(results.Select(x => x.ToDto()));
	}
}