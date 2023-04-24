using IndyPOS.Application.Abstractions.Messaging;
using IndyPOS.Application.Common.Interfaces;
using IndyPOS.Domain.Entities;
using MediatR;

namespace IndyPOS.Application.Invoices.Commands.CreateInvoice;

public class CreateInvoiceCommandHandler : ICommandHandler<CreateInvoiceCommand>
{
    private readonly IInvoiceRepository _invoiceRepository;

    public CreateInvoiceCommandHandler(IInvoiceRepository invoiceRepository)
    {
        _invoiceRepository = invoiceRepository;
    }

    public Task<Unit> Handle(CreateInvoiceCommand command, CancellationToken cancellationToken)
    {
        var entity = new Invoice
        {
            Total = command.Total,
            CustomerId = command.CustomerId,
            UserId = command.UserId
        };

		_invoiceRepository.Add(entity);

        return Task.FromResult(Unit.Value);
    }
}