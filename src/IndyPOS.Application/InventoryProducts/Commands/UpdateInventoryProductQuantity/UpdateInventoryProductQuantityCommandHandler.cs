using IndyPOS.Application.Abstractions.Messaging;
using IndyPOS.Application.Common.Interfaces;
using IndyPOS.Domain.Events;
using Prism.Events;

namespace IndyPOS.Application.InventoryProducts.Commands.UpdateInventoryProductQuantity;

public class UpdateInventoryProductQuantityCommandHandler : ICommandHandler<UpdateInventoryProductQuantityCommand>
{
	private readonly IInventoryProductRepository _productRepository;
	private readonly IEventAggregator _eventAggregator;

	public UpdateInventoryProductQuantityCommandHandler(IInventoryProductRepository productRepository, 
														IEventAggregator eventAggregator)
    {
        _productRepository = productRepository;
        _eventAggregator = eventAggregator;
    }

	public Task Handle(UpdateInventoryProductQuantityCommand command, CancellationToken cancellationToken)
	{
		_ = _productRepository.UpdateProductQuantityById(command.Id, command.Quantity);

		_eventAggregator.GetEvent<InventoryProductUpdatedEvent>().Publish(command.Id);

		return Task.CompletedTask;
	}
}