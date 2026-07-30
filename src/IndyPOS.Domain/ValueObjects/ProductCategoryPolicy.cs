using IndyPOS.Domain.Enums;

namespace IndyPOS.Domain.ValueObjects;

/// <summary>
/// Whether a store may use a category of a given kind. The single place this rule lives, so the
/// create handler, the update handler and the UI cannot drift apart.
/// </summary>
public static class ProductCategoryPolicy
{
    public static bool IsUsable(ProductCategoryKind kind, StoreTypeFeatures features) =>
        kind == ProductCategoryKind.GeneralGoods || features.MultipleProductTypesEnabled;
}
