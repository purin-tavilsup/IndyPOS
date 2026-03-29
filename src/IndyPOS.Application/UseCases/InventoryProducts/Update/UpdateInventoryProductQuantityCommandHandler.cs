using IndyPOS.Application.Abstractions.Pos.Repositories;
using IndyPOS.Application.Common.Helpers;
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

		var guid = LegacyIdHelper.ToGuid(command.Id);
		_eventAggregator.GetEvent<InventoryProductUpdatedEvent>().Publish(guid);

		return Task.CompletedTask;
	}
}