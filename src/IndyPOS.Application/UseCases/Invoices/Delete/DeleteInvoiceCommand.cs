using IndyPOS.Application.Abstractions.Messaging;

namespace IndyPOS.Application.UseCases.Invoices.Delete;

public record DeleteInvoiceCommand(int Id) : ICommand;