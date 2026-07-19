using Nokpirab;

namespace IndyPOS.Application.UseCases.StoreHub.PaymentMethods;

public class TogglePaymentMethodCommandHandler
    : ICommandHandler<TogglePaymentMethodCommand, PaymentMethodMutationResponse>
{
    private readonly IPaymentMethodCatalogService _catalogService;

    public TogglePaymentMethodCommandHandler(IPaymentMethodCatalogService catalogService)
    {
        _catalogService = catalogService;
    }

    public async Task<PaymentMethodMutationResponse> HandleAsync(
        TogglePaymentMethodCommand command,
        CancellationToken cancellationToken = default)
    {
        await _catalogService.SetEnabledAsync(command.Code, command.Enabled, cancellationToken);
        return new PaymentMethodMutationResponse(command.Code);
    }
}
