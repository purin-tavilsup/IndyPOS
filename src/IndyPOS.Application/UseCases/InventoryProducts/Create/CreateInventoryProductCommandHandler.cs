using IndyPOS.Application.Abstractions.Pos.Repositories;
using IndyPOS.Domain.Events;
using Nokpirab;

namespace IndyPOS.Application.UseCases.InventoryProducts.Create;

public class CreateInventoryProductCommandHandler : ICommandHandler<CreateInventoryProductCommand>
{
	private readonly IInventoryProductRepository _productRepository;
	private readonly IEventAggregator _eventAggregator;

	public CreateInventoryProductCommandHandler(IInventoryProductRepository productRepository,
												IEventAggregator eventAggregator)
	{
		_productRepository = productRepository;
		_eventAggregator = eventAggregator;
	}

	public Task HandleAsync(CreateInventoryProductCommand command, CancellationToken cancellationToken = default)
	{
		var id = _productRepository.Add(command.ToEntity());

		_eventAggregator.GetEvent<InventoryProductAddedEvent>().Publish(id);

		return Task.CompletedTask;
	}
}