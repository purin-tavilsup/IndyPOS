using IndyPOS.Application.Abstractions.Messaging;
using IndyPOS.Application.Abstractions.Pos.Repositories;
using IndyPOS.Domain.Events;

namespace IndyPOS.Application.UseCases.InventoryProducts.Update;

public class UpdateInventoryProductCommandHandler : ICommandHandler<UpdateInventoryProductCommand>
{
	private readonly IInventoryProductRepository _productRepository;
	private readonly IEventAggregator _eventAggregator;

	public UpdateInventoryProductCommandHandler(IInventoryProductRepository productRepository,
												IEventAggregator eventAggregator)
	{
		_productRepository = productRepository;
		_eventAggregator = eventAggregator;
	}

	public Task Handle(UpdateInventoryProductCommand command, CancellationToken cancellationToken)
	{
		_ = _productRepository.Update(command.ToEntity());

		_eventAggregator.GetEvent<InventoryProductUpdatedEvent>().Publish(command.Id);

		return Task.CompletedTask;
	}
}