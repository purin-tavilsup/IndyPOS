using IndyPOS.Application.Abstractions.Messaging;
using IndyPOS.Application.Common.Interfaces;
using IndyPOS.Domain.Entities;
using IndyPOS.Domain.Events;
using MediatR;
using Prism.Events;

namespace IndyPOS.Application.InvoiceProducts.Commands.CreateInvoiceProduct;

public class CreateInvoiceProductCommandHandler : ICommandHandler<CreateInvoiceProductCommand>
{
    private readonly IInvoiceProductRepository _invoiceProductRepository;
    private readonly IEventAggregator _eventAggregator;

    public CreateInvoiceProductCommandHandler(IInvoiceProductRepository invoiceProductRepository, IEventAggregator eventAggregator)
    {
        _invoiceProductRepository = invoiceProductRepository;
        _eventAggregator = eventAggregator;
    }

    public Task<Unit> Handle(CreateInvoiceProductCommand command, CancellationToken cancellationToken)
    {
        var entity = new InvoiceProduct
        {
            InvoiceId = command.InvoiceId,
            Priority = command.Priority,
            InventoryProductId = command.InventoryProductId,
            Barcode = command.Barcode,
            Description = command.Description,
            Manufacturer = command.Manufacturer,
            Brand = command.Brand,
            Category = command.Category,
            UnitPrice = command.UnitPrice,
            Quantity = command.Quantity,
            Note = command.Note
        };

		_invoiceProductRepository.Add(entity);

        _eventAggregator.GetEvent<InvoiceProductAddedEvent>().Publish();

        return Task.FromResult(Unit.Value);
    }
}