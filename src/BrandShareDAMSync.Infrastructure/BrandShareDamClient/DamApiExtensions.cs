using BrandshareDamSync.Infrastructure.BrandShareDamClient;
using BrandShareDAMSync.Infrastructure.Utils;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace BrandshareDamSync.Infrastructure.BrandShareDamClient;

public static class DamApiExtensions
{
    public static IAsyncEnumerable<ModifiedItem> StreamModifiedItems(
    this IBrandShareDamApi api,
    string tenantId,
    string jobId,
    string volumeName,
    bool isActive,
    string? lastItemId = null,
    int pageSize = 100,
    long? modifiedAfterAt = null,
    Action<long?>? onLastRunTime = null,
    CancellationToken ct = default)
    {
        var reported = false;

        return Pager.StreamPaged<ModifiedItem>(
            fetch: async (cursor, size, token) =>
            {
                var resp = await api.GetModifiedItems(
                    tenantId, jobId, volumeName, isActive,
                    cursor, size, modifiedAfterAt, token
                ).ConfigureAwait(false);

                if (!resp.IsSuccessStatusCode)
                    throw new HttpRequestException($"Failed: {(int)resp.StatusCode} {resp.ReasonPhrase}");

                if (!reported)
                {
                    onLastRunTime?.Invoke(resp.Content?.LastRunTime);
                    reported = true;
                }

                var items = (IReadOnlyList<ModifiedItem>?)resp.Content?.Items
                            ?? Array.Empty<ModifiedItem>();

                string? next = null;
                if (items.Count > 0)
                {
                    var last = items[^1];
                    next = last?.ItemId;
                    if (cursor != null && next == cursor)
                        next = null; // avoid loops
                }

                return (items, next);
            },
            startCursor: lastItemId,
            pageSize: pageSize,
            ct: ct);
    }

}
