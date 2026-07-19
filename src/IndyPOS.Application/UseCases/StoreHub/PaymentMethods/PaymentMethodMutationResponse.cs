namespace IndyPOS.Application.UseCases.StoreHub.PaymentMethods;

/// <summary>
/// Minimal response for the payment-method admin mutation commands (add
/// campaign, toggle, edit display). Echoes the Code acted on since these
/// commands have no other result to report.
/// </summary>
public record PaymentMethodMutationResponse(string Code);
