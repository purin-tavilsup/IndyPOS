using Nokpirab;

namespace IndyPOS.Application.UseCases.StoreHub.PaymentMethods;

public class EditPaymentMethodDisplayCommandHandler
    : ICommandHandler<EditPaymentMethodDisplayCommand, PaymentMethodMutationResponse>
{
    private readonly IPaymentMethodCatalogService _catalogService;

    public EditPaymentMethodDisplayCommandHandler(IPaymentMethodCatalogService catalogService)
    {
        _catalogService = catalogService;
    }

    public async Task<PaymentMethodMutationResponse> HandleAsync(
        EditPaymentMethodDisplayCommand command,
        CancellationToken cancellationToken = default)
    {
        await _catalogService.UpdateDisplayAsync(command.Code, command.DisplayName, command.DisplayOrder, cancellationToken);
        return new PaymentMethodMutationResponse(command.Code);
    }
}
