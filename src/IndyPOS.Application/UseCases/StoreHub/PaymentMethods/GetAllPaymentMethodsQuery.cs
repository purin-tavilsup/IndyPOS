using Nokpirab;

namespace IndyPOS.Application.UseCases.StoreHub.PaymentMethods;

/// <summary>
/// Query to retrieve every payment-method row for this store, enabled and
/// disabled alike. Used by the admin screen (unlike the offerable query,
/// which is checkout-facing and policy-filtered).
/// </summary>
public record GetAllPaymentMethodsQuery : IQuery<IReadOnlyList<PaymentMethodDto>>;
