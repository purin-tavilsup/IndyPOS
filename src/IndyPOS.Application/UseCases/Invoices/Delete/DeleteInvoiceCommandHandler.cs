using IndyPOS.Application.Common.Interfaces;
using Nokpirab;

namespace IndyPOS.Application.UseCases.Invoices.Delete;

public class DeleteInvoiceCommandHandler : ICommandHandler<DeleteInvoiceCommand>
{
	private readonly IInvoiceRepository _invoiceRepository;

	public DeleteInvoiceCommandHandler(IInvoiceRepository invoiceRepository)
	{
		_invoiceRepository = invoiceRepository;
	}

	public Task HandleAsync(DeleteInvoiceCommand command, CancellationToken cancellationToken = default)
	{
		_invoiceRepository.RemoveById(command.Id);

		return Task.CompletedTask;
	}
}