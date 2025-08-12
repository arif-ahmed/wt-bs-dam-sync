using BrandshareDamSync.Core.Models;

namespace BrandshareDamSync.Infrastructure.Config;

public interface IConfigStore
{
    Task<MachineConfig> LoadAsync(CancellationToken ct);
    Task SaveAsync(MachineConfig config, CancellationToken ct);
}
