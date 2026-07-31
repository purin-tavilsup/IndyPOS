namespace IndyPOS.Domain.Enums;

/// <summary>
/// Classifies a catalogue product category. Carries the product-type gate that used to be a
/// string comparison against an enum name.
/// <para>Backing values are persisted (see ProductCategoryConfiguration), so never reuse one.</para>
/// </summary>
public enum ProductCategoryKind
{
    /// <summary>Everyday retail goods. Every store type may sell these.</summary>
    GeneralGoods = 1,

    /// <summary>Building materials and tools. GeneralHardware only.</summary>
    Hardware = 2,

    /// <summary>
    /// A service rather than stock (e.g. MimyShop's บริการ). Recorded so the category has a
    /// home; non-stock BEHAVIOUR is not implemented and needs Product.IsTrackable.
    /// </summary>
    Service = 3
}
