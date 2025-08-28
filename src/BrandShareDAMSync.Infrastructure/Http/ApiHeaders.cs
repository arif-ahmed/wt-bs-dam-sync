namespace BrandshareDamSync.Daemon.Infrastructure.Http;

public static class ApiHeaders
{
    // Use whatever your upstream expects for API key:
    public const string ApiKey = "X-ApiKey";
    public const string TenantId = "X-Tenant-Id"; // how we pick tenants per request
}
