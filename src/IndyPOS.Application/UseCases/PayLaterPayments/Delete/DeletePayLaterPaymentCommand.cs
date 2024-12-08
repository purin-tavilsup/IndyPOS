using IndyPOS.Application.Abstractions.Messaging;

namespace IndyPOS.Application.UseCases.PayLaterPayments.Delete;

public record DeletePayLaterPaymentCommand(int Id) : ICommand;