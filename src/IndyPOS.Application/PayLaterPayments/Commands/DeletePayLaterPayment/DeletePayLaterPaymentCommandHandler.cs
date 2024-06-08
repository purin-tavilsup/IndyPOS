using IndyPOS.Application.Abstractions.Messaging;
using IndyPOS.Application.Abstractions.Pos.Repositories;

namespace IndyPOS.Application.PayLaterPayments.Commands.DeletePayLaterPayment;

public class DeletePayLaterPaymentCommandHandler : ICommandHandler<DeletePayLaterPaymentCommand>
{
	private readonly IPayLaterPaymentRepository _payLaterPaymentRepository;

	public DeletePayLaterPaymentCommandHandler(IPayLaterPaymentRepository payLaterPaymentRepository)
	{
		_payLaterPaymentRepository = payLaterPaymentRepository;
	}

	public Task Handle(DeletePayLaterPaymentCommand command, CancellationToken cancellationToken)
	{
		_payLaterPaymentRepository.RemoveById(command.Id);

		return Task.CompletedTask;
	}
}