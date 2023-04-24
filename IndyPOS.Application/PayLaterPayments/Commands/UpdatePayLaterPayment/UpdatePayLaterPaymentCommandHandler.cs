using IndyPOS.Application.Abstractions.Messaging;
using IndyPOS.Application.Common.Interfaces;
using IndyPOS.Domain.Entities;
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
		var entity = new PayLaterPayment
		{
			PaymentId = command.PaymentId,
			PaidAmount = command.PaidAmount,
			IsCompleted = command.IsCompleted
		};

		_payLaterPaymentRepository.Update(entity);

		return Task.FromResult(Unit.Value);
	}
}