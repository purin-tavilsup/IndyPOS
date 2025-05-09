using IndyPOS.Application.Abstractions.Pos.Repositories;
using Nokpirab;

namespace IndyPOS.Application.UseCases.PayLaterPayments.Delete;

public class DeletePayLaterPaymentCommandHandler : ICommandHandler<DeletePayLaterPaymentCommand>
{
	private readonly IPayLaterPaymentRepository _payLaterPaymentRepository;

	public DeletePayLaterPaymentCommandHandler(IPayLaterPaymentRepository payLaterPaymentRepository)
	{
		_payLaterPaymentRepository = payLaterPaymentRepository;
	}

	public Task HandleAsync(DeletePayLaterPaymentCommand command, CancellationToken cancellationToken = default)
	{
		_payLaterPaymentRepository.RemoveById(command.Id);

		return Task.CompletedTask;
	}
}