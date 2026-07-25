namespace IndyPOS.Domain.Enums;

/// <summary>
/// Classifies a catalog payment method for display. Carries no behaviour —
/// offerability is decided by <see cref="ValueObjects.PaymentMethodPolicy"/>.
/// Backing values are persisted (see PaymentMethodConfiguration), so never reuse one.
/// </summary>
public enum PaymentMethodKind
{
    /// <summary>Everyday tender that always applies (cash, bank transfer).</summary>
    Standard = 1,

    /// <summary>A time-boxed government subsidy scheme; churns roughly yearly.</summary>
    GovernmentCampaign = 2,

    /// <summary>Store-specific arrangement rather than tender — e.g. PayLater (ลงบัญชี).</summary>
    Special = 3
}
