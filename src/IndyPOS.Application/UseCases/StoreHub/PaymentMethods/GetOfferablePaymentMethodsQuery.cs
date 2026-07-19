using Nokpirab;

namespace IndyPOS.Application.UseCases.StoreHub.PaymentMethods;

/// <summary>
/// Query to retrieve the payment methods offerable at the checkout for this
/// store — enabled rows, filtered by the store-type policy (e.g. PayLater is
/// GeneralHardware-only), ordered for display.
/// </summary>
public record GetOfferablePaymentMethodsQuery : IQuery<IReadOnlyList<PaymentMethodDto>>;
