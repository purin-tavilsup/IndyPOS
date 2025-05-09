using IndyPOS.Application.Abstractions.Pos.Repositories;
using Nokpirab;

namespace IndyPOS.Application.UseCases.InvoiceProducts.Delete;

public class DeleteInvoiceProductCommandHandler : ICommandHandler<DeleteInvoiceProductCommand>
{
	private readonly IInvoiceProductRepository _invoiceProductRepository;

	public DeleteInvoiceProductCommandHandler(IInvoiceProductRepository invoiceProductRepository)
	{
		_invoiceProductRepository = invoiceProductRepository;
	}

	public Task HandleAsync(DeleteInvoiceProductCommand command, CancellationToken cancellationToken = default)
	{
		_invoiceProductRepository.RemoveById(command.Id);

		return Task.CompletedTask;
	}
}