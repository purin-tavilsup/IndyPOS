using IndyPOS.Application.Abstractions.Messaging;
using IndyPOS.Application.Common.Interfaces;
using MediatR;

namespace IndyPOS.Application.PayLaterPayments.Commands.UpdatePayLaterPayment;

public class UpdatePayLaterPaymentCommandHandler : ICommandHandler<UpdatePayLaterPaymentCommand>
{
	private readonly IPayLaterPaymentRepository _payLaterPaymentRepository;

	public UpdatePayLaterPaymentCommandHandler(IPayLaterPaymentRepository payLaterPaymentRepository)
    {
        _payLaterPaymentRepository = payLaterPaymentRepository;
    }

	public Task<Unit> Handle(UpdatePayLaterPaymentCommand command, CancellationToken cancellationToken)
	{
		_payLaterPaymentRepository.Update(command.ToEntity());

		return Task.FromResult(Unit.Value);
	}
}