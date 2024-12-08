using IndyPOS.Application.Abstractions.Messaging;
using IndyPOS.Application.Abstractions.Pos.Repositories;

namespace IndyPOS.Application.UseCases.PayLaterPayments.Update;

public class UpdatePayLaterPaymentCommandHandler : ICommandHandler<UpdatePayLaterPaymentCommand>
{
	private readonly IPayLaterPaymentRepository _payLaterPaymentRepository;

	public UpdatePayLaterPaymentCommandHandler(IPayLaterPaymentRepository payLaterPaymentRepository)
    {
        _payLaterPaymentRepository = payLaterPaymentRepository;
    }

	public Task Handle(UpdatePayLaterPaymentCommand command, CancellationToken cancellationToken)
	{
		_payLaterPaymentRepository.Update(command.ToEntity());

		return Task.CompletedTask;
	}
}