using FluentValidation;

namespace IndyPOS.Application.InvoicePayments.Commands.DeleteInvoicePayment;

public class DeleteInvoicePaymentCommandValidator : AbstractValidator<DeleteInvoicePaymentCommand>
{
	public DeleteInvoicePaymentCommandValidator()
	{
		RuleFor(x => x.Id)
			.GreaterThan(0).WithMessage("Payment ID is invalid.");
	}
}