using FluentValidation;

namespace IndyPOS.Application.PayLaterPayments.Commands.CreatePayLaterPayment;

public class CreatePayLaterPaymentCommandValidator : AbstractValidator<CreatePayLaterPaymentCommand>
{
	public CreatePayLaterPaymentCommandValidator()
	{
		RuleFor(x => x.InvoiceId)
			.GreaterThan(0).WithMessage("Invoice ID is invalid.");

		RuleFor(x => x.ReceivableAmount)
			.GreaterThan(0m).WithMessage("Pay Later Amount is invalid.");
	}
}