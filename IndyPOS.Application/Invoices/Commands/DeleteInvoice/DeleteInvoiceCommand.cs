using IndyPOS.Application.Abstractions.Messaging;

namespace IndyPOS.Application.Invoices.Commands.DeleteInvoice;

public record DeleteInvoiceCommand(int Id) : ICommand;