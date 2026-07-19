using Nokpirab;

namespace IndyPOS.Application.UseCases.StoreHub.PaymentMethods;

/// <summary>
/// Command to edit a payment method's display name and/or display order.
/// </summary>
public record EditPaymentMethodDisplayCommand(string Code, string DisplayName, int? DisplayOrder)
    : ICommand<PaymentMethodMutationResponse>;
