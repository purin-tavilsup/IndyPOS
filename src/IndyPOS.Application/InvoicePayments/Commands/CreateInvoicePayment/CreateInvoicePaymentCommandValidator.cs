using FluentValidation;

namespace IndyPOS.Application.InvoicePayments.Commands.CreateInvoicePayment;

public class CreateInvoicePaymentCommandValidator : AbstractValidator<CreateInvoicePaymentCommand>
{
    public CreateInvoicePaymentCommandValidator()
    {
		RuleFor(x => x.InvoiceId)
			.GreaterThan(0).WithMessage("Invoice ID is invalid.");

		RuleFor(x => x.PaymentTypeId)
			.GreaterThan(0).WithMessage("Payment Type ID is invalid.");
    }
}