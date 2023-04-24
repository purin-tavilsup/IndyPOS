using IndyPOS.Application.Abstractions.Messaging;

namespace IndyPOS.Application.PayLaterPayments.Commands.DeletePayLaterPayment;

public record DeletePayLaterPaymentCommand(int Id) : ICommand;