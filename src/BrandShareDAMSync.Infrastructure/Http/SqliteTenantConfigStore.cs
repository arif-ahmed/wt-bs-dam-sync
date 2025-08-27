using System;
using System.Threading;
using System.Threading.Tasks;
using BrandshareDamSync.Infrastructure.Persistence.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;


namespace BrandshareDamSync.Daemon.Infrastructure.Http;

public sealed class SqliteTenantConfigStore : ITenantConfigStore
{
    private readonly DamSyncDbContext _db;
    private readonly IMemoryCache _cache;
    private readonly ILogger<SqliteTenantConfigStore> _logger;

    private static readonly TimeSpan CacheTtl = TimeSpan.FromMinutes(5);
    private static readonly TimeSpan CacheFailTtl = TimeSpan.FromSeconds(30);
    private static readonly string CachePrefix = "tenant-config:";

    public SqliteTenantConfigStore(
        DamSyncDbContext db,
        IMemoryCache cache,
        ILogger<SqliteTenantConfigStore> logger)
    {
        _db = db;
        _cache = cache;
        _logger = logger;
    }

    public async Task<(string BaseUrl, string ApiKey)?> GetByTenantIdAsync(string tenantId, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(tenantId))
            return null;

        var key = CachePrefix + tenantId.ToUpperInvariant();

        if (_cache.TryGetValue(key, out (string BaseUrl, string ApiKey)? cached))
            return cached;

        try
        {
            var row = await _db.Tenants
                .AsNoTracking()
                .FirstOrDefaultAsync(t => t.Id == tenantId && !t.IsDeleted, ct);

            if (row is null)
            {
                _logger.LogWarning("Tenant '{TenantId}' not found or inactive.", tenantId);
                _cache.Set<(string, string)?>(key, null, CacheFailTtl);
                return null;
            }

            var result = (row.BaseUrl, row.ApiKey);
            _cache.Set(key, result, CacheTtl);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load tenant '{TenantId}' from SQLite.", tenantId);
            _cache.Set<(string, string)?>(key, null, CacheFailTtl);
            return null;
        }
    }
}
