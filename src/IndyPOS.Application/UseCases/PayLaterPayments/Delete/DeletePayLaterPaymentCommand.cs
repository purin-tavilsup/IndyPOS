using Nokpirab;

namespace IndyPOS.Application.UseCases.PayLaterPayments.Delete;

public record DeletePayLaterPaymentCommand(int Id) : ICommand;