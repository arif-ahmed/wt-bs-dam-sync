namespace BrandshareDamSync.Abstractions;

public interface IDownloaderService
{
    event Action<int>? ProgressChanged;
    event Action<string>? DownloadCompleted;
    Task DownloadFileAsync(string url, string destinationPath, CancellationToken ct = default);
}
