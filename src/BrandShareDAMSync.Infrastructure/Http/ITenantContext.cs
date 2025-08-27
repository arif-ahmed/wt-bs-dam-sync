namespace BrandshareDamSync.Daemon.Infrastructure.Http;

public interface ITenantContext
{
    string? TenantId { get; }
    string? ApiKey { get; }
    string? BaseUrl { get; }

    IDisposable Use(string tenantId, string apiKey, string baseUrl);
}
