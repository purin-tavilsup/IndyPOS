using IndyPOS.Application.Abstractions.Pos.Repositories;
using IndyPOS.Domain.Events;
using Nokpirab;

namespace IndyPOS.Application.UseCases.InventoryProducts.Update;

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

	public Task HandleAsync(UpdateInventoryProductQuantityCommand command, CancellationToken cancellationToken = default)
	{
		_ = _productRepository.UpdateProductQuantityById(command.Id, command.Quantity);

		_eventAggregator.GetEvent<InventoryProductUpdatedEvent>().Publish(command.Id);

		return Task.CompletedTask;
	}
}