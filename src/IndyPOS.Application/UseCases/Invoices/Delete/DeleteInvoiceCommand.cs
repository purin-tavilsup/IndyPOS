using Nokpirab;

namespace IndyPOS.Application.UseCases.Invoices.Delete;

public record DeleteInvoiceCommand(int Id) : ICommand;