using Nokpirab;

namespace IndyPOS.Application.UseCases.InvoiceProducts.Delete;

public record DeleteInvoiceProductCommand(int Id) : ICommand;