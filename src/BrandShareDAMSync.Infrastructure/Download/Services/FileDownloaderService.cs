using BrandshareDamSync.Infrastructure.Download.Abstractions;
using BrandshareDamSync.Infrastructure.Download.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Collections.Concurrent;

namespace BrandshareDamSync.Infrastructure.Download.Services;

public sealed class FileDownloaderService : IFileDownloaderService, IDisposable
{
    private readonly IDownloaderService _downloader;
    private readonly ILogger<FileDownloaderService> _logger;
    private readonly SemaphoreSlim _gate;
    private readonly ConcurrentDictionary<string, byte> _inFlightByTargetPath = new();
    private readonly int _maxAttempts;
    private readonly TimeSpan _initialBackoff;
    private bool _disposed;

    public FileDownloaderService(
        IDownloaderService downloader,
        IOptions<FileDownloadOptions> options,
        ILogger<FileDownloaderService> logger)
    {
        _downloader = downloader;
        _logger = logger;

        var opts = options.Value ?? new FileDownloadOptions();
        if (opts.MaxConcurrent < 1) throw new ArgumentOutOfRangeException(nameof(opts.MaxConcurrent));
        if (opts.MaxAttempts < 1) throw new ArgumentOutOfRangeException(nameof(opts.MaxAttempts));
        if (opts.InitialBackoffSeconds <= 0) throw new ArgumentOutOfRangeException(nameof(opts.InitialBackoffSeconds));

        _gate = new SemaphoreSlim(opts.MaxConcurrent, opts.MaxConcurrent);
        _maxAttempts = opts.MaxAttempts;
        _initialBackoff = TimeSpan.FromSeconds(opts.InitialBackoffSeconds);
    }

    public async Task DownloadAsync(string sourceUrl, string destinationPath, CancellationToken ct = default)
    {
        ThrowIfDisposed();

        if (!_inFlightByTargetPath.TryAdd(destinationPath, 0))
        {
            _logger.LogInformation("Skipping duplicate in-flight download: {Path}", destinationPath);
            return;
        }

        await _gate.WaitAsync(ct);
        try
        {
            await DownloadCoreAsync(sourceUrl, destinationPath, ct);
        }
        finally
        {
            _gate.Release();
            _inFlightByTargetPath.TryRemove(destinationPath, out _);
        }
    }

    public async Task DownloadManyAsync(IEnumerable<(string sourceUrl, string destinationPath)> jobs, CancellationToken ct = default)
    {
        ThrowIfDisposed();
        var tasks = jobs.Select(j => DownloadAsync(j.sourceUrl, j.destinationPath, ct)).ToArray();
        await Task.WhenAll(tasks);
    }

    private async Task DownloadCoreAsync(string sourceUrl, string destinationPath, CancellationToken ct)
    {
        var delay = _initialBackoff;

        for (int attempt = 1; ; attempt++)
        {
            void OnProgress(int progress)
                => _logger.LogInformation("Downloading {Url} -> {Path} … {Progress}%", sourceUrl, destinationPath, progress);

            void OnCompleted(string filePath)
                => _logger.LogInformation("✔ File downloaded to temp: {TempPath}", filePath);

            _downloader.ProgressChanged += OnProgress;
            _downloader.DownloadCompleted += OnCompleted;

            try
            {
                var tmp = destinationPath + ".downloading";
                if (File.Exists(tmp)) File.Delete(tmp);

                await _downloader.DownloadFileAsync(sourceUrl, tmp, ct);

                Directory.CreateDirectory(Path.GetDirectoryName(destinationPath)!);

                if (File.Exists(destinationPath))
                {
                    var bak = destinationPath + ".bak";
                    try
                    {
                        if (File.Exists(bak)) File.Delete(bak);
                        File.Replace(tmp, destinationPath, bak, ignoreMetadataErrors: true);
                        try { File.Delete(bak); } catch { /* ignore */ }
                    }
                    catch
                    {
                        File.Move(tmp, destinationPath, overwrite: true);
                    }
                }
                else
                {
                    File.Move(tmp, destinationPath);
                }

                _logger.LogInformation("✅ Download success: {Path}", destinationPath);
                break;
            }
            catch (OperationCanceledException) { throw; }
            catch (Exception ex) when (attempt < _maxAttempts)
            {
                _logger.LogWarning(ex, "Download failed (attempt {Attempt}/{Max}) for {Path}. Retrying in {Delay}…",
                                   attempt, _maxAttempts, destinationPath, delay);
                await Task.Delay(delay, ct);
                delay = TimeSpan.FromSeconds(delay.TotalSeconds * 2);
            }
            finally
            {
                _downloader.ProgressChanged -= OnProgress;
                _downloader.DownloadCompleted -= OnCompleted;
            }
        }
    }

    private void ThrowIfDisposed()
    {
        if (_disposed) throw new ObjectDisposedException(nameof(FileDownloaderService));
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _gate.Dispose();
    }
}