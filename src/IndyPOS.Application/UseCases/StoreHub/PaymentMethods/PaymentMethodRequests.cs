namespace IndyPOS.Application.UseCases.StoreHub.PaymentMethods;

/// <summary>
/// Request body for adding a new government-campaign payment method to the catalog.
/// </summary>
public record AddCampaignPaymentMethodRequest(string Code, string DisplayName, int DisplayOrder);

/// <summary>
/// Request body for updating an existing payment method's enabled state and/or
/// display fields. Fields left null are not changed.
/// </summary>
public record UpdatePaymentMethodRequest(bool? IsEnabled, string? DisplayName, int? DisplayOrder);
