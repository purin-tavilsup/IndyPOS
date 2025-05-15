using IndyPOS.Application.Common.Interfaces;
using Nokpirab;

namespace IndyPOS.Application.UseCases.Invoices.Create;

public class CreateInvoiceCommandHandler : ICommandHandler<CreateInvoiceCommand, int>
{
    private readonly IInvoiceRepository _invoiceRepository;

    public CreateInvoiceCommandHandler(IInvoiceRepository invoiceRepository)
    {
        _invoiceRepository = invoiceRepository;
    }

    public Task<int> HandleAsync(CreateInvoiceCommand command, CancellationToken cancellationToken = default)
    {
        var id = _invoiceRepository.Add(command.ToEntity());

        return Task.FromResult(id);
    }
}