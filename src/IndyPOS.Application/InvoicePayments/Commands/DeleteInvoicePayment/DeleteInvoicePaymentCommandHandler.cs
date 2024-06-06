using IndyPOS.Application.Abstractions.Messaging;
using IndyPOS.Application.Abstractions.Pos.Repositories;

namespace IndyPOS.Application.InvoicePayments.Commands.DeleteInvoicePayment;

public class DeleteInvoicePaymentCommandHandler  : ICommandHandler<DeleteInvoicePaymentCommand>
{
	private readonly IInvoicePaymentRepository _invoicePaymentRepository;

	public DeleteInvoicePaymentCommandHandler(IInvoicePaymentRepository invoicePaymentRepository)
	{
		_invoicePaymentRepository = invoicePaymentRepository;
	}

	public Task Handle(DeleteInvoicePaymentCommand command, CancellationToken cancellationToken)
	{
		_invoicePaymentRepository.RemoveById(command.Id);

		return Task.CompletedTask;
	}
}