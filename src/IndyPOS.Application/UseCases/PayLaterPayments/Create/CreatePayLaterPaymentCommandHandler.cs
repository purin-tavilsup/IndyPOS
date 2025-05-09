using IndyPOS.Application.Abstractions.Pos.Repositories;
using Nokpirab;

namespace IndyPOS.Application.UseCases.PayLaterPayments.Create;

public class CreatePayLaterPaymentCommandHandler : ICommandHandler<CreatePayLaterPaymentCommand>
{
	private readonly IPayLaterPaymentRepository _payLaterPaymentRepository;

    public CreatePayLaterPaymentCommandHandler(IPayLaterPaymentRepository payLaterPaymentRepository)
    {
        _payLaterPaymentRepository = payLaterPaymentRepository;
    }

	public Task HandleAsync(CreatePayLaterPaymentCommand command, CancellationToken cancellationToken = default)
	{
		_payLaterPaymentRepository.Add(command.ToEntity());

		return Task.CompletedTask;
	}
}