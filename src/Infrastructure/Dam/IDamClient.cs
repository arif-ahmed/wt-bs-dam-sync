namespace BrandshareDamSync.Infrastructure.Dam;

public interface IDamClient
{
    Task<List<string>> ListDamAssetsAsync(string damFolderId, CancellationToken ct);
    Task UploadAsync(string damFolderId, string filePath, CancellationToken ct);
    Task DownloadAsync(string damFolderId, string destinationPath, CancellationToken ct);
    Task DeleteFromDamAsync(string damFolderId, string assetId, CancellationToken ct);
    Task<List<BrandshareDamSync.Core.Models.Job>> PollNewJobsAsync(BrandshareDamSync.Core.Models.MachineConfig machine, CancellationToken ct);
}
