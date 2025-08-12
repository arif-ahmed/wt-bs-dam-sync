using BrandshareDamSync.Core.Models;
using BrandshareDamSync.Infrastructure.Dam;

namespace BrandshareDamSync.Core.Strategies;

public sealed class OneWayDownloadJob : JobStrategyBase
{
    public override JobDirection Direction => JobDirection.OneWayDownload;

    public override async Task ExecuteAsync(Job job, IDamClient dam, RuntimeState state, CancellationToken ct)
    {
        await dam.DownloadAsync(job.DamFolderId, job.LocalFolder, ct);
        state.JobStatuses[job.Id] = "Downloaded assets";
    }
}
