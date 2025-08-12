using BrandshareDamSync.Core.Models;
using BrandshareDamSync.Infrastructure.Dam;

namespace BrandshareDamSync.Core.Strategies;

public sealed class UploadAndCleanJob : JobStrategyBase
{
    public override JobDirection Direction => JobDirection.UploadAndClean;

    public override async Task ExecuteAsync(Job job, IDamClient dam, RuntimeState state, CancellationToken ct)
    {
        int count = 0;
        await ForEachFileAsync(job.LocalFolder, async file =>
        {
            await dam.UploadAsync(job.DamFolderId, file, ct);
            try { File.Delete(file); } catch { }
            count++;
        }, ct);
        state.JobStatuses[job.Id] = $"Uploaded & cleaned {count} file(s)";
    }
}
