﻿using IndyPOS.Application.Abstractions.Messaging;
using IndyPOS.Application.Common.Interfaces;
using IndyPOS.Domain.Entities;
using IndyPOS.Domain.Events;
using MediatR;
using Prism.Events;

namespace IndyPOS.Application.InvoicePayments.Commands.CreateInvoicePayment;

public class CreateInvoicePaymentCommandHandler : ICommandHandler<CreateInvoicePaymentCommand>
{
    private readonly IInvoicePaymentRepository _invoicePaymentRepository;
    private readonly IEventAggregator _eventAggregator;

    public CreateInvoicePaymentCommandHandler(IInvoicePaymentRepository invoicePaymentRepository, IEventAggregator eventAggregator)
    {
        _invoicePaymentRepository = invoicePaymentRepository;
        _eventAggregator = eventAggregator;
    }

    public Task<Unit> Handle(CreateInvoicePaymentCommand command, CancellationToken cancellationToken)
	{
		var entity = new Payment
		{
			InvoiceId = command.InvoiceId,
			PaymentTypeId = command.PaymentTypeId,
			Amount = command.Amount,
			Note = command.Note
		};

		_invoicePaymentRepository.Add(entity);

		_eventAggregator.GetEvent<InvoicePaymentAddedEvent>().Publish();

        return Task.FromResult(Unit.Value);
    }
}