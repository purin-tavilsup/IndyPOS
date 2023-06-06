using IndyPOS.Application.Abstractions.Messaging;

namespace IndyPOS.Application.InvoicePayments.Commands.DeleteInvoicePayment;

public record DeleteInvoicePaymentCommand(int Id) : ICommand;