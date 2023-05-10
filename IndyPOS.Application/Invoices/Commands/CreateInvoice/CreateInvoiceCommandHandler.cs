using IndyPOS.Application.Abstractions.Messaging;
using IndyPOS.Application.Common.Interfaces;
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
		_invoiceRepository.Add(command.ToEntity());

        return Task.FromResult(Unit.Value);
    }
}