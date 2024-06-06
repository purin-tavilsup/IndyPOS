using IndyPOS.Application.Abstractions.Messaging;
using IndyPOS.Application.Abstractions.Pos.Repositories;

namespace IndyPOS.Application.PayLaterPayments.Commands.CreatePayLaterPayment;

public class CreatePayLaterPaymentCommandHandler : ICommandHandler<CreatePayLaterPaymentCommand>
{
	private readonly IPayLaterPaymentRepository _payLaterPaymentRepository;

    public CreatePayLaterPaymentCommandHandler(IPayLaterPaymentRepository payLaterPaymentRepository)
    {
        _payLaterPaymentRepository = payLaterPaymentRepository;
    }

    public Task Handle(CreatePayLaterPaymentCommand command, CancellationToken cancellationToken)
    {
		_payLaterPaymentRepository.Add(command.ToEntity());

		return Task.CompletedTask;
    }
}