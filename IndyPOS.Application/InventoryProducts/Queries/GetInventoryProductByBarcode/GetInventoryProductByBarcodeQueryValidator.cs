using FluentValidation;

namespace IndyPOS.Application.InventoryProducts.Queries.GetInventoryProductByBarcode;

public class GetInventoryProductByBarcodeQueryValidator : AbstractValidator<GetInventoryProductByBarcodeQuery>
{
	public GetInventoryProductByBarcodeQueryValidator()
	{
		RuleFor(x => x.Barcode)
			.NotEmpty().WithMessage("Barcode is required.");
	}
}