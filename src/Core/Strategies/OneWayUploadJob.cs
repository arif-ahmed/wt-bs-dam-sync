using BrandshareDamSync.Core.Models;
using BrandshareDamSync.Infrastructure.Dam;

namespace BrandshareDamSync.Core.Strategies;

public sealed class OneWayUploadJob : JobStrategyBase
{
    public override JobDirection Direction => JobDirection.OneWayUpload;

    public override async Task ExecuteAsync(Job job, IDamClient dam, RuntimeState state, CancellationToken ct)
    {
        int count = 0;
        await ForEachFileAsync(job.LocalFolder, async file => { await dam.UploadAsync(job.DamFolderId, file, ct); count++; }, ct);
        state.JobStatuses[job.Id] = $"Uploaded {count} file(s)";
    }
}
