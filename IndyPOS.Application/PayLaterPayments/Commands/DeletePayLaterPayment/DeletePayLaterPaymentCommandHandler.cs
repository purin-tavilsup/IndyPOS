using IndyPOS.Application.Abstractions.Messaging;
using IndyPOS.Application.Common.Interfaces;
using MediatR;

namespace IndyPOS.Application.PayLaterPayments.Commands.DeletePayLaterPayment;

public class DeletePayLaterPaymentCommandHandler : ICommandHandler<DeletePayLaterPaymentCommand>
{
	private readonly IPayLaterPaymentRepository _payLaterPaymentRepository;

	public DeletePayLaterPaymentCommandHandler(IPayLaterPaymentRepository payLaterPaymentRepository)
	{
		_payLaterPaymentRepository = payLaterPaymentRepository;
	}

	public Task<Unit> Handle(DeletePayLaterPaymentCommand command, CancellationToken cancellationToken)
	{
		_payLaterPaymentRepository.RemoveById(command.Id);

		return Task.FromResult(Unit.Value);
	}
}