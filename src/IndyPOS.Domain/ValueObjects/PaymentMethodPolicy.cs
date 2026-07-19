using IndyPOS.Domain.Entities.Core;
using IndyPOS.Domain.Enums;

namespace IndyPOS.Domain.ValueObjects;

/// <summary>
/// Pure rule (no I/O) deciding which catalog methods are offerable for a store
/// type. PayLater is GeneralHardware-only as a hard code invariant, independent
/// of the row's IsEnabled — so a hand-edited catalog cannot enable it elsewhere.
/// </summary>
public static class PaymentMethodPolicy
{
    private const string PayLaterCode = "PayLater";

    public static IReadOnlyList<PaymentMethod> Offerable(
        IEnumerable<PaymentMethod> methods, StoreType storeType) =>
        methods
            .Where(m => IsOfferable(m, storeType))
            .OrderBy(m => m.DisplayOrder)
            .ToList();

    public static bool IsOfferable(PaymentMethod method, StoreType storeType)
    {
        if (!method.IsEnabled) return false;
        if (IsPayLater(method) && storeType != StoreType.GeneralHardware) return false;
        return true;
    }

    private static bool IsPayLater(PaymentMethod method) =>
        string.Equals(method.Code, PayLaterCode, StringComparison.OrdinalIgnoreCase);
}
