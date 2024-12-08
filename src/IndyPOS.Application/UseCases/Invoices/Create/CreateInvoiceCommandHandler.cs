using IndyPOS.Application.Abstractions.Messaging;
using IndyPOS.Application.Common.Interfaces;

namespace IndyPOS.Application.UseCases.Invoices.Create;

public class CreateInvoiceCommandHandler : ICommandHandler<CreateInvoiceCommand, int>
{
    private readonly IInvoiceRepository _invoiceRepository;

    public CreateInvoiceCommandHandler(IInvoiceRepository invoiceRepository)
    {
        _invoiceRepository = invoiceRepository;
    }

    public Task<int> Handle(CreateInvoiceCommand command, CancellationToken cancellationToken)
    {
		var id = _invoiceRepository.Add(command.ToEntity());

        return Task.FromResult(id);
    }
}