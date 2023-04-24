using IndyPOS.Application.Abstractions.Messaging;
using IndyPOS.Application.Common.Interfaces;
using MediatR;

namespace IndyPOS.Application.InvoiceProducts.Commands.DeleteInvoiceProduct;

public class DeleteInvoiceProductCommandHandler : ICommandHandler<DeleteInvoiceProductCommand>
{
	private readonly IInvoiceProductRepository _invoiceProductRepository;

	public DeleteInvoiceProductCommandHandler(IInvoiceProductRepository invoiceProductRepository)
	{
		_invoiceProductRepository = invoiceProductRepository;
	}

	public Task<Unit> Handle(DeleteInvoiceProductCommand command, CancellationToken cancellationToken)
	{
		_invoiceProductRepository.RemoveById(command.Id);

		return Task.FromResult(Unit.Value);
	}
}