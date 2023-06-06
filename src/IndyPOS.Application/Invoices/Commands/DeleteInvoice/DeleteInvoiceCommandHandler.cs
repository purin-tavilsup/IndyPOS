using IndyPOS.Application.Abstractions.Messaging;
using IndyPOS.Application.Common.Interfaces;

namespace IndyPOS.Application.Invoices.Commands.DeleteInvoice;

public class DeleteInvoiceCommandHandler : ICommandHandler<DeleteInvoiceCommand>
{
	private readonly IInvoiceRepository _invoiceRepository;

	public DeleteInvoiceCommandHandler(IInvoiceRepository invoiceRepository)
	{
		_invoiceRepository = invoiceRepository;
	}

	public Task Handle(DeleteInvoiceCommand command, CancellationToken cancellationToken)
	{
		_invoiceRepository.RemoveById(command.Id);

		return Task.CompletedTask;
	}
}