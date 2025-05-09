using IndyPOS.Application.Abstractions.Pos.Repositories;
using Nokpirab;

namespace IndyPOS.Application.UseCases.InvoicePayments.Delete;

public class DeleteInvoicePaymentCommandHandler  : ICommandHandler<DeleteInvoicePaymentCommand>
{
	private readonly IInvoicePaymentRepository _invoicePaymentRepository;

	public DeleteInvoicePaymentCommandHandler(IInvoicePaymentRepository invoicePaymentRepository)
	{
		_invoicePaymentRepository = invoicePaymentRepository;
	}

	public Task HandleAsync(DeleteInvoicePaymentCommand command, CancellationToken cancellationToken = default)
	{
		_invoicePaymentRepository.RemoveById(command.Id);

		return Task.CompletedTask;
	}
}