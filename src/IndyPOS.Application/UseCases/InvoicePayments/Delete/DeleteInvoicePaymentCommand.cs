using Nokpirab;

namespace IndyPOS.Application.UseCases.InvoicePayments.Delete;

public record DeleteInvoicePaymentCommand(int Id) : ICommand;