using BrandshareDamSync.Core.Models;

namespace BrandshareDamSync.Infrastructure.Dam;

public sealed class FakeDamClient : IDamClient
{
    public Task<List<string>> ListDamAssetsAsync(string damFolderId, CancellationToken ct)
        => Task.FromResult(new List<string> { "asset1", "asset2", "asset3" });

    public async Task UploadAsync(string damFolderId, string filePath, CancellationToken ct)
        => await Task.Delay(100, ct);

    public async Task DownloadAsync(string damFolderId, string destinationPath, CancellationToken ct)
    {
        await Task.Delay(100, ct);
        Directory.CreateDirectory(destinationPath);
        var stub = Path.Combine(destinationPath, $"downloaded_{Guid.NewGuid():N}.bin");
        await File.WriteAllBytesAsync(stub, new byte[1024], ct);
    }

    public Task DeleteFromDamAsync(string damFolderId, string assetId, CancellationToken ct)
        => Task.CompletedTask;

    public Task<List<Job>> PollNewJobsAsync(MachineConfig machine, CancellationToken ct)
    {
        if (DateTimeOffset.UtcNow.Second % 30 == 0)
        {
            return Task.FromResult(new List<Job>
            {
                new Job { Name = "Polled Upload", DamFolderId = "marketing/images", LocalFolder = "./samples", SyncIntervalMinutes = 5, Direction = JobDirection.OneWayUpload }
            });
        }
        return Task.FromResult(new List<Job>());
    }
}
