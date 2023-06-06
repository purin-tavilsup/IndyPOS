using FluentValidation;

namespace IndyPOS.Application.Invoices.Commands.CreateInvoice;

public class CreateInvoiceCommandValidator : AbstractValidator<CreateInvoiceCommand>
{
    public CreateInvoiceCommandValidator()
    {
		RuleFor(x => x.UserId)
			.GreaterThan(0).WithMessage("User ID is invalid.");
    }
}