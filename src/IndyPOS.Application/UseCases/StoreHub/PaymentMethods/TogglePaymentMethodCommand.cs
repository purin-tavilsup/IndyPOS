using Nokpirab;

namespace IndyPOS.Application.UseCases.StoreHub.PaymentMethods;

/// <summary>
/// Command to enable or disable a payment method (e.g. retiring a
/// government-campaign method once it ends).
/// </summary>
public record TogglePaymentMethodCommand(string Code, bool Enabled) : ICommand<PaymentMethodMutationResponse>;
