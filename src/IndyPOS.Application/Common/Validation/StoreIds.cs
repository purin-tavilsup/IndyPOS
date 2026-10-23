using System.Text.RegularExpressions;

namespace IndyPOS.Application.Common.Validation;

/// <summary>
/// What a store id may be. It becomes a URL segment in Cloud.Auth's store-client API and part of the store's client
/// id, and "/" or "%" are decoded differently along the way: "a/b" and "a%2Fb" could land on one Auth client, so one
/// registration would rotate another store's secret. Letters, digits, "-" and "_", at most 50 characters, as both
/// databases' keys hold.
/// </summary>
public static partial class StoreIds
{
    public const int MaxLength = 50;

    public const string Rule = "letters, digits, '-' and '_' only, 1 to 50 characters";

    public static bool IsValid(string? storeId) => storeId is not null && Pattern().IsMatch(storeId);

    [GeneratedRegex("^[A-Za-z0-9_-]{1,50}$")]
    private static partial Regex Pattern();
}
