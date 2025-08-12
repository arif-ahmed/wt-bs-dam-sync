namespace BrandshareDamSync.Infrastructure.Secrets;

public interface ISecretStore
{
    Task SetAsync(string key, string secret, CancellationToken ct);
    Task<string?> GetAsync(string key, CancellationToken ct);
}
