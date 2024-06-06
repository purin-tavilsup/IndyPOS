using FluentValidation;

namespace IndyPOS.Application.InventoryProducts.Queries.GetById;

public class GetInventoryProductByIdQueryValidator : AbstractValidator<GetInventoryProductByIdQuery>
{
	public GetInventoryProductByIdQueryValidator()
	{
		RuleFor(x => x.Id)
			.GreaterThan(0).WithMessage("Inventory Product Id is invalid.");
	}
}