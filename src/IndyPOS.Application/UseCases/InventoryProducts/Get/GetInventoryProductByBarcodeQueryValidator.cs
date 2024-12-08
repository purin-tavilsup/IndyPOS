using FluentValidation;

namespace IndyPOS.Application.UseCases.InventoryProducts.Get;

public class GetInventoryProductByBarcodeQueryValidator : AbstractValidator<GetInventoryProductByBarcodeQuery>
{
	public GetInventoryProductByBarcodeQueryValidator()
	{
		RuleFor(x => x.Barcode)
			.NotEmpty().WithMessage("Barcode is required.");
	}
}