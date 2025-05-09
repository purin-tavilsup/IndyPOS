using IndyPOS.Application.Abstractions.Pos.Repositories;
using Nokpirab;

namespace IndyPOS.Application.UseCases.PayLaterPayments.Update;

public class UpdatePayLaterPaymentCommandHandler : ICommandHandler<UpdatePayLaterPaymentCommand>
{
	private readonly IPayLaterPaymentRepository _payLaterPaymentRepository;

	public UpdatePayLaterPaymentCommandHandler(IPayLaterPaymentRepository payLaterPaymentRepository)
    {
        _payLaterPaymentRepository = payLaterPaymentRepository;
    }

	public Task HandleAsync(UpdatePayLaterPaymentCommand command, CancellationToken cancellationToken = default)
	{
		_payLaterPaymentRepository.Update(command.ToEntity());

		return Task.CompletedTask;
	}
}