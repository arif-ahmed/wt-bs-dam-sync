namespace BrandshareDamSync.Infrastructure.Download.Abstractions;
public interface IDownloaderService
{
    event Action<int>? ProgressChanged;
    event Action<string>? DownloadCompleted;

    Task DownloadFileAsync(string sourceUrl, string destinationPath, CancellationToken ct = default);
}

