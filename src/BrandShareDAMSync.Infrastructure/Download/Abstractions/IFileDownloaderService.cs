namespace BrandshareDamSync.Infrastructure.Download.Abstractions;

public interface IFileDownloaderService
{
    Task DownloadAsync(string sourceUrl, string destinationPath, CancellationToken ct = default);
    Task DownloadManyAsync(IEnumerable<(string sourceUrl, string destinationPath)> jobs, CancellationToken ct = default);
}
