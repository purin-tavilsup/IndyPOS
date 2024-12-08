using IndyPOS.Application.Abstractions.Messaging;
using IndyPOS.Application.Abstractions.Pos.Repositories;
using IndyPOS.Domain.Events;

namespace IndyPOS.Application.UseCases.InventoryProducts.Delete;

public class DeleteInventoryProductCommandHandler : ICommandHandler<DeleteInventoryProductCommand>
{
	private readonly IInventoryProductRepository _productRepository;
	private readonly IEventAggregator _eventAggregator;

	public DeleteInventoryProductCommandHandler(IInventoryProductRepository productRepository,
												IEventAggregator eventAggregator)
	{
		_productRepository = productRepository;
		_eventAggregator = eventAggregator;
	}

	public Task Handle(DeleteInventoryProductCommand command, CancellationToken cancellationToken)
	{
		_productRepository.RemoveById(command.Id);

		_eventAggregator.GetEvent<InventoryProductDeletedEvent>().Publish();

		return Task.CompletedTask;
	}
}