using Nokpirab;

namespace IndyPOS.Application.UseCases.StoreHub.PaymentMethods;

public class AddCampaignPaymentMethodCommandHandler
    : ICommandHandler<AddCampaignPaymentMethodCommand, PaymentMethodMutationResponse>
{
    private readonly IPaymentMethodCatalogService _catalogService;

    public AddCampaignPaymentMethodCommandHandler(IPaymentMethodCatalogService catalogService)
    {
        _catalogService = catalogService;
    }

    public async Task<PaymentMethodMutationResponse> HandleAsync(
        AddCampaignPaymentMethodCommand command,
        CancellationToken cancellationToken = default)
    {
        await _catalogService.AddCampaignAsync(command.Code, command.DisplayName, command.DisplayOrder, cancellationToken);
        return new PaymentMethodMutationResponse(command.Code);
    }
}
