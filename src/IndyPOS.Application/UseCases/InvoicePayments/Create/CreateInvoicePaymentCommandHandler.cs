using IndyPOS.Application.Abstractions.Pos.Repositories;
using IndyPOS.Domain.Events;
using Nokpirab;

namespace IndyPOS.Application.UseCases.InvoicePayments.Create;

public class CreateInvoicePaymentCommandHandler : ICommandHandler<CreateInvoicePaymentCommand, int>
{
    private readonly IInvoicePaymentRepository _invoicePaymentRepository;
    private readonly IEventAggregator _eventAggregator;

    public CreateInvoicePaymentCommandHandler(IInvoicePaymentRepository invoicePaymentRepository, IEventAggregator eventAggregator)
    {
        _invoicePaymentRepository = invoicePaymentRepository;
        _eventAggregator = eventAggregator;
    }

	public Task<int> HandleAsync(CreateInvoicePaymentCommand command, CancellationToken cancellationToken = default)
	{
		var id = _invoicePaymentRepository.Add(command.ToEntity());

		_eventAggregator.GetEvent<InvoicePaymentAddedEvent>().Publish();

		return Task.FromResult(id);
	}
}