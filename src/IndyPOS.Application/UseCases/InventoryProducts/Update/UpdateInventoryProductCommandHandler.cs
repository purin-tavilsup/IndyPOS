using IndyPOS.Application.Abstractions.Pos.Repositories;
using IndyPOS.Application.Common.Helpers;
using IndyPOS.Domain.Events;
using Nokpirab;

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

	public Task HandleAsync(UpdateInventoryProductCommand command, CancellationToken cancellationToken = default)
	{
		_ = _productRepository.Update(command.ToEntity());

		var guid = LegacyIdHelper.ToGuid(command.Id);
		_eventAggregator.GetEvent<InventoryProductUpdatedEvent>().Publish(guid);

		return Task.CompletedTask;
	}
}