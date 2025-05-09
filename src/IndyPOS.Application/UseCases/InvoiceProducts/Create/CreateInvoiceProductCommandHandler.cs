using IndyPOS.Application.Abstractions.Pos.Repositories;
using IndyPOS.Domain.Events;
using Nokpirab;

namespace IndyPOS.Application.UseCases.InvoiceProducts.Create;

public class CreateInvoiceProductCommandHandler : ICommandHandler<CreateInvoiceProductCommand>
{
    private readonly IInvoiceProductRepository _invoiceProductRepository;
    private readonly IEventAggregator _eventAggregator;

    public CreateInvoiceProductCommandHandler(IInvoiceProductRepository invoiceProductRepository, IEventAggregator eventAggregator)
    {
        _invoiceProductRepository = invoiceProductRepository;
        _eventAggregator = eventAggregator;
    }

	public Task HandleAsync(CreateInvoiceProductCommand command, CancellationToken cancellationToken = default)
	{
		_invoiceProductRepository.Add(command.ToEntity());

		_eventAggregator.GetEvent<InvoiceProductAddedEvent>().Publish();

		return Task.CompletedTask;
	}
}