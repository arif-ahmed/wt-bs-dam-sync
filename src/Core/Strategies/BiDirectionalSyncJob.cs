using BrandshareDamSync.Core.Models;
using BrandshareDamSync.Infrastructure.Dam;

namespace BrandshareDamSync.Core.Strategies;

public sealed class BiDirectionalSyncJob : JobStrategyBase
{
    public override JobDirection Direction => JobDirection.BiDirectional;

    public override async Task ExecuteAsync(Job job, IDamClient dam, RuntimeState state, CancellationToken ct)
    {
        await Task.Delay(200, ct);
        state.JobStatuses[job.Id] = "Bi-directional sync completed (simulated)";
    }
}
