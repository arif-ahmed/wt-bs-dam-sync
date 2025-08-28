namespace BrandShareDAMSync.Infrastructure.Utils;

public static class Pager
{
    /// <summary>
    /// Minimal async paginator: cursor-in, items-out.
    /// Uses IReadOnlyList so Array.Empty<T>() and List<T> both fit naturally.
    /// </summary>
    public static async IAsyncEnumerable<T> StreamPaged<T>(
        Func<string?, int, CancellationToken, Task<(IReadOnlyList<T> Items, string? NextCursor)>> fetch,
        string? startCursor = null,
        int pageSize = 100,
        int maxPages = 10_000,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken ct = default)
    {
        var seen = new HashSet<string?>(StringComparer.Ordinal);
        var cursor = string.IsNullOrWhiteSpace(startCursor) ? null : startCursor;
        var pages = 0;

        while (!ct.IsCancellationRequested && pages++ < maxPages)
        {
            var (items, next) = await fetch(cursor, pageSize, ct).ConfigureAwait(false);

            if (items is null || items.Count == 0)
                yield break;

            foreach (var item in items)
                yield return item;

            // stop if no cursor, loop detected, or unchanged cursor
            if (next is null || !seen.Add(next) || next == cursor)
                yield break;

            cursor = next;
        }
    }
}
