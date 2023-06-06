using IndyPOS.Application.Abstractions.Messaging;

namespace IndyPOS.Application.InvoiceProducts.Commands.DeleteInvoiceProduct;

public record DeleteInvoiceProductCommand(int Id) : ICommand;