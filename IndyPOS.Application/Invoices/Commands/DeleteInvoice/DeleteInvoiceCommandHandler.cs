using IndyPOS.Application.Abstractions.Messaging;
using IndyPOS.Application.Common.Interfaces;
using MediatR;

namespace IndyPOS.Application.Invoices.Commands.DeleteInvoice;

public class DeleteInvoiceCommandHandler : ICommandHandler<DeleteInvoiceCommand>
{
	private readonly IInvoiceRepository _invoiceRepository;

	public DeleteInvoiceCommandHandler(IInvoiceRepository invoiceRepository)
	{
		_invoiceRepository = invoiceRepository;
	}

	public Task<Unit> Handle(DeleteInvoiceCommand command, CancellationToken cancellationToken)
	{
		_invoiceRepository.RemoveById(command.Id);

		return Task.FromResult(Unit.Value);
	}
}