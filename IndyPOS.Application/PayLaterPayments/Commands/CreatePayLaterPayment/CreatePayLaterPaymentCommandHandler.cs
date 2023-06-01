using IndyPOS.Application.Abstractions.Messaging;
using IndyPOS.Application.Common.Interfaces;
using MediatR;

namespace IndyPOS.Application.PayLaterPayments.Commands.CreatePayLaterPayment;

public class CreatePayLaterPaymentCommandHandler : ICommandHandler<CreatePayLaterPaymentCommand>
{
	private readonly IPayLaterPaymentRepository _payLaterPaymentRepository;

    public CreatePayLaterPaymentCommandHandler(IPayLaterPaymentRepository payLaterPaymentRepository)
    {
        _payLaterPaymentRepository = payLaterPaymentRepository;
    }

    public Task<Unit> Handle(CreatePayLaterPaymentCommand command, CancellationToken cancellationToken)
    {
		_payLaterPaymentRepository.Add(command.ToEntity());

		return Task.FromResult(Unit.Value);
    }
}