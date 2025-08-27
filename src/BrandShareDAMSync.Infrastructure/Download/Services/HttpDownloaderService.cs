
using BrandshareDamSync.Abstractions;
using BrandshareDamSync.Infrastructure.Download.Abstractions;

namespace BrandshareDamSync.Infrastructure.Download.Services;

public sealed class HttpDownloaderService : BrandshareDamSync.Abstractions.IDownloaderService, IDisposable
{
    private readonly HttpClient _http;
    private bool _disposed;

    public event Action<int>? ProgressChanged;
    public event Action<string>? DownloadCompleted;

    public HttpDownloaderService(HttpClient? http = null)
        => _http = http ?? new HttpClient();

    public async Task DownloadFileAsync(string sourceUrl, string destinationPath, CancellationToken ct = default)
    {
        ThrowIfDisposed();

        using var req = new HttpRequestMessage(HttpMethod.Get, sourceUrl);
        using var resp = await _http.SendAsync(req, HttpCompletionOption.ResponseHeadersRead, ct);
        resp.EnsureSuccessStatusCode();

        var contentLength = resp.Content.Headers.ContentLength;
        await using var stream = await resp.Content.ReadAsStreamAsync(ct);

        var dir = Path.GetDirectoryName(destinationPath);
        if (!string.IsNullOrWhiteSpace(dir)) Directory.CreateDirectory(dir);

        const int bufferSize = 81920;
        var buffer = new byte[bufferSize];
        long totalRead = 0;
        int lastReported = -1;

        await using var fs = new FileStream(destinationPath, FileMode.Create, FileAccess.Write, FileShare.None, bufferSize, useAsync: true);

        while (true)
        {
            var read = await stream.ReadAsync(buffer.AsMemory(0, buffer.Length), ct);
            if (read == 0) break;

            await fs.WriteAsync(buffer.AsMemory(0, read), ct);
            totalRead += read;

            if (contentLength.HasValue && contentLength.Value > 0)
            {
                var pct = (int)Math.Clamp((totalRead * 100L) / contentLength.Value, 0, 100);
                if (pct != lastReported)
                {
                    lastReported = pct;
                    ProgressChanged?.Invoke(pct);
                }
            }
        }

        if (!contentLength.HasValue) ProgressChanged?.Invoke(100);
        DownloadCompleted?.Invoke(destinationPath);
    }

    private void ThrowIfDisposed()
    {
        if (_disposed) throw new ObjectDisposedException(nameof(HttpDownloaderService));
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _http.Dispose();
    }
}