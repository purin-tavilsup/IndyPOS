namespace IndyPOS.Domain.ValueObjects;

/// <summary>
/// Thai banknotes and coins, largest first. One place for their values, so a saved count's total and
/// the till's live total while counting cannot disagree.
/// </summary>
public static class CashDenominations
{
    public static IReadOnlyList<decimal> Values { get; } = [1000m, 500m, 100m, 50m, 20m, 10m, 5m, 2m, 1m];

    /// <summary>The value of the counts, given in <see cref="Values"/> order (1000 down to 1).</summary>
    public static decimal Total(IReadOnlyList<int> counts)
    {
        if (counts.Count != Values.Count)
            throw new ArgumentException($"Expected {Values.Count} counts, one per denomination, but got {counts.Count}.", nameof(counts));

        return counts.Zip(Values, (count, value) => count * value).Sum();
    }
}
