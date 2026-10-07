namespace IndyPOS.Application.Common.Models;

/// <summary>
/// Reads a paged list to the end. The cap only stops a server that always says "more" from looping the till
/// for ever, so it sits far above real data: GeneralHardware has sold 325,780 lines in all, 1,629 pages of 200.
/// </summary>
public static class PageReader
{
    public const int MaxPages = 10_000;

    public static async Task<IReadOnlyList<T>> ReadAllAsync<T>(Func<int, Task<(IReadOnlyList<T> Items, bool HasMore)>> readPage)
    {
        var all = new List<T>();
        for (var page = 1; page <= MaxPages; page++)
        {
            var (items, hasMore) = await readPage(page);
            all.AddRange(items);
            if (!hasMore)
                return all;
        }

        throw new InvalidOperationException($"The report did not end within {MaxPages} pages.");
    }
}
