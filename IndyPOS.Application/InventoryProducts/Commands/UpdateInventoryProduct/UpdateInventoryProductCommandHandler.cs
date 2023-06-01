using IndyPOS.Application.Abstractions.Messaging;
using IndyPOS.Application.Common.Interfaces;
using IndyPOS.Domain.Events;
using MediatR;
using Prism.Events;

namespace IndyPOS.Application.InventoryProducts.Commands.UpdateInventoryProduct;

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

	public Task<Unit> Handle(UpdateInventoryProductCommand command, CancellationToken cancellationToken)
	{
		_ = _productRepository.Update(command.ToEntity());

		_eventAggregator.GetEvent<InventoryProductUpdatedEvent>().Publish(command.Id);

		return Task.FromResult(Unit.Value);
	}
}