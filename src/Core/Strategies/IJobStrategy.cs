using BrandshareDamSync.Core.Models;
using BrandshareDamSync.Infrastructure.Dam;

namespace BrandshareDamSync.Core.Strategies;

public interface IJobStrategy
{
    JobDirection Direction { get; }
    Task ExecuteAsync(Job job, IDamClient dam, RuntimeState state, CancellationToken ct);
}
