using IndyPOS.Application.Abstractions.Messaging;

namespace IndyPOS.Application.UseCases.InvoicePayments.Delete;

public record DeleteInvoicePaymentCommand(int Id) : ICommand;