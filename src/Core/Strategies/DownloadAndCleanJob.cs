using BrandshareDamSync.Core.Models;
using BrandshareDamSync.Infrastructure.Dam;

namespace BrandshareDamSync.Core.Strategies;

public sealed class DownloadAndCleanJob : JobStrategyBase
{
    public override JobDirection Direction => JobDirection.DownloadAndClean;

    public override async Task ExecuteAsync(Job job, IDamClient dam, RuntimeState state, CancellationToken ct)
    {
        var assets = await dam.ListDamAssetsAsync(job.DamFolderId, ct);
        await dam.DownloadAsync(job.DamFolderId, job.LocalFolder, ct);
        foreach (var a in assets) await dam.DeleteFromDamAsync(job.DamFolderId, a, ct);
        state.JobStatuses[job.Id] = $"Downloaded {assets.Count} & cleaned from DAM";
    }
}
