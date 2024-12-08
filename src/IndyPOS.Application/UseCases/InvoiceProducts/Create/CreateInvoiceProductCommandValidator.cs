using FluentValidation;

namespace IndyPOS.Application.UseCases.InvoiceProducts.Create;

public class CreateInvoiceProductCommandValidator : AbstractValidator<CreateInvoiceProductCommand>
{
    public CreateInvoiceProductCommandValidator()
    {
		RuleFor(x => x.InvoiceId)
			.GreaterThan(0).WithMessage("Invoice ID is invalid.");

		RuleFor(x => x.Barcode)
			.NotEmpty().WithMessage("Product barcode cannot be empty.");

		RuleFor(x => x.Description)
			.NotEmpty().WithMessage("Product description cannot be empty.");

		RuleFor(x => x.Category)
			.GreaterThan(0).WithMessage("Product category is invalid.");

		RuleFor(x => x.UnitPrice)
			.GreaterThan(-1m).WithMessage("Product unit price is invalid.");
	}
}