using IndyPOS.Application.Abstractions.Messaging;
using IndyPOS.Application.Common.Interfaces;
using IndyPOS.Domain.Events;
using MediatR;
using Prism.Events;

namespace IndyPOS.Application.InventoryProducts.Commands.CreateInventoryProduct;

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

    public Task<Unit> Handle(CreateInventoryProductCommand command, CancellationToken cancellationToken)
    {
		var id = _productRepository.Add(command.ToEntity());

		_eventAggregator.GetEvent<InventoryProductAddedEvent>().Publish(id);

		return Task.FromResult(Unit.Value);
	}
}