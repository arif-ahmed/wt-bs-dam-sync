using System;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace BrandshareDamSync.Daemon.Infrastructure.Http;

public sealed class TenantConfigHandler : DelegatingHandler
{
    private readonly ITenantConfigStore _store;
    private readonly ITenantContext _ctx;
    private readonly ILogger<TenantConfigHandler> _logger;

    public TenantConfigHandler(
        ITenantConfigStore store,
        ITenantContext ctx,
        ILogger<TenantConfigHandler> logger)
    {
        _store = store;
        _ctx = ctx;
        _logger = logger;
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
    {
        // 1) Tenant from header
        var tenantId = GetTenantIdFromHeader(request);

        // 2) or ambient context
        tenantId ??= _ctx.TenantId;

        if (string.IsNullOrWhiteSpace(tenantId))
        {
            _logger.LogDebug("No tenantId (header/context). Skipping tenant resolution.");
            return await base.SendAsync(request, ct);
        }

        var cfg = await _store.GetByTenantIdAsync(tenantId!, ct);
        if (cfg is null)
        {
            _logger.LogWarning("No tenant config found for tenantId '{TenantId}'.", tenantId);
            return await base.SendAsync(request, ct);
        }

        // Rewrite RequestUri to the tenant BaseUrl
        if (!string.IsNullOrWhiteSpace(cfg.Value.BaseUrl))
        {
            var baseUri = new Uri(cfg.Value.BaseUrl, UriKind.Absolute);
            var pathAndQuery = request.RequestUri?.PathAndQuery ?? "/";
            request.RequestUri = new Uri(baseUri, pathAndQuery);
        }

        // Inject API key if missing
        if (!request.Headers.Contains(ApiHeaders.ApiKey) && !string.IsNullOrWhiteSpace(cfg.Value.ApiKey))
        {
            request.Headers.TryAddWithoutValidation(ApiHeaders.ApiKey, cfg.Value.ApiKey);
        }

        return await base.SendAsync(request, ct);
    }

    private static string? GetTenantIdFromHeader(HttpRequestMessage request)
    {
        return request.Headers.TryGetValues(ApiHeaders.TenantId, out var values)
            ? values.FirstOrDefault()
            : null;
    }
}
