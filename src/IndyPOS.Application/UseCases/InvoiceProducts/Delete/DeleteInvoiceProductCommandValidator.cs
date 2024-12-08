using FluentValidation;

namespace IndyPOS.Application.UseCases.InvoiceProducts.Delete;

public class DeleteInvoiceProductCommandValidator : AbstractValidator<DeleteInvoiceProductCommand>
{
	public DeleteInvoiceProductCommandValidator()
	{
		RuleFor(x => x.Id)
			.GreaterThan(0).WithMessage("Invoice Product ID is invalid.");
	}
}