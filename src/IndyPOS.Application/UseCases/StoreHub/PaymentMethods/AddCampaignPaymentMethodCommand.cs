using Nokpirab;

namespace IndyPOS.Application.UseCases.StoreHub.PaymentMethods;

/// <summary>
/// Command to add a new government-campaign payment method to the catalog.
/// The new row is created enabled (Kind = GovernmentCampaign).
/// </summary>
public record AddCampaignPaymentMethodCommand(string Code, string DisplayName, int DisplayOrder)
    : ICommand<PaymentMethodMutationResponse>;
