using IndyPOS.Application.Abstractions.Messaging;
using IndyPOS.Application.Common.Interfaces;
using IndyPOS.Domain.Entities;
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
		var entity = new PayLaterPayment
		{
			PaymentId = command.PaymentId,
			Description = command.Description,
			InvoiceId = command.InvoiceId,
			ReceivableAmount = command.ReceivableAmount
		};

		_payLaterPaymentRepository.Add(entity);

		return Task.FromResult(Unit.Value);
    }
}