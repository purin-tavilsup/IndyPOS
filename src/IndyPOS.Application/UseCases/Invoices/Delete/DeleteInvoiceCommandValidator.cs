using FluentValidation;

namespace IndyPOS.Application.UseCases.Invoices.Delete;

public class DeleteInvoiceCommandValidator : AbstractValidator<DeleteInvoiceCommand>
{
	public DeleteInvoiceCommandValidator()
	{
		RuleFor(x => x.Id)
			.GreaterThan(0).WithMessage("Invoice ID is invalid.");
	}
}