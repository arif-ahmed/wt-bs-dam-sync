using System.Threading;
using System.Threading.Tasks;

namespace BrandshareDamSync.Daemon.Infrastructure.Http;

public interface ITenantConfigStore
{
    Task<(string BaseUrl, string ApiKey)?> GetByTenantIdAsync(string tenantId, CancellationToken ct = default);
}
