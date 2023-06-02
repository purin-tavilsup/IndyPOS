using IndyPOS.Application.Abstractions.Messaging;
using IndyPOS.Application.Common.Interfaces;
using MediatR;

namespace IndyPOS.Application.InvoicePayments.Commands.DeleteInvoicePayment;

public class DeleteInvoicePaymentCommandHandler  : ICommandHandler<DeleteInvoicePaymentCommand>
{
	private readonly IInvoicePaymentRepository _invoicePaymentRepository;

	public DeleteInvoicePaymentCommandHandler(IInvoicePaymentRepository invoicePaymentRepository)
	{
		_invoicePaymentRepository = invoicePaymentRepository;
	}

	public Task<Unit> Handle(DeleteInvoicePaymentCommand command, CancellationToken cancellationToken)
	{
		_invoicePaymentRepository.RemoveById(command.Id);

		return Task.FromResult(Unit.Value);
	}
}