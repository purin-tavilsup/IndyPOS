﻿using IndyPOS.Application.Abstractions.Messaging;
using IndyPOS.Application.Common.Interfaces;
using IndyPOS.Domain.Events;
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

    public Task Handle(CreateInvoiceProductCommand command, CancellationToken cancellationToken)
    {
		_invoiceProductRepository.Add(command.ToEntity());

        _eventAggregator.GetEvent<InvoiceProductAddedEvent>().Publish();

		return Task.CompletedTask;
    }
}