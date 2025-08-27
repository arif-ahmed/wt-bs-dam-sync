using BrandshareDamSync.Abstractions;

namespace BrandshareDamSync.Infrastructure;

//public class HttpDownloaderService : IDownloaderService
//{
//    private readonly HttpClient _httpClient;

//    public HttpDownloaderService(HttpClient httpClient)
//    {
//        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
//    }

//    // Event for progress updates (0–100 %)
//    public event Action<int>? ProgressChanged;

//    // Event when download is complete (final absolute path)
//    public event Action<string>? DownloadCompleted;

//    public async Task DownloadFileAsync(string url, string destinationPath, CancellationToken ct = default)
//    {
//        if (string.IsNullOrWhiteSpace(url)) throw new ArgumentException("URL must be provided.", nameof(url));
//        if (string.IsNullOrWhiteSpace(destinationPath)) throw new ArgumentException("Destination path must be provided.", nameof(destinationPath));

//        // Ensure destination directory exists
//        var finalFullPath = Path.GetFullPath(destinationPath);
//        var destDir = Path.GetDirectoryName(finalFullPath)!;
//        Directory.CreateDirectory(destDir);

//        // Download to a temp file first, then atomically move
//        var tempPath = finalFullPath + ".part";

//        // Make sure any stale temp file is gone (best-effort)
//        TryDeleteFileIfExists(tempPath);

//        using var request = new HttpRequestMessage(HttpMethod.Get, url);
//        using HttpResponseMessage response = await _httpClient.SendAsync(
//            request,
//            HttpCompletionOption.ResponseHeadersRead,
//            ct
//        ).ConfigureAwait(false);

//        response.EnsureSuccessStatusCode();

//        // Progress setup
//        var totalBytes = response.Content.Headers.ContentLength ?? -1L;
//        var canReportProgress = totalBytes > 0;
//        long totalRead = 0;
//        int lastProgress = -1;

//        // Open output stream with sharing that allows readers
//        await WithRetriesAsync(async () =>
//        {
//            await using Stream httpStream = await response.Content.ReadAsStreamAsync(ct).ConfigureAwait(false);
//            await using var fileStream = new FileStream(
//                tempPath,
//                FileMode.Create,
//                FileAccess.Write,
//                FileShare.Read,
//                bufferSize: 1024 * 128, // 128 KiB
//                useAsync: true);

//            var buffer = new byte[1024 * 128];
//            int bytesRead;

//            while ((bytesRead = await httpStream.ReadAsync(buffer.AsMemory(0, buffer.Length), ct).ConfigureAwait(false)) > 0)
//            {
//                await fileStream.WriteAsync(buffer.AsMemory(0, bytesRead), ct).ConfigureAwait(false);
//                totalRead += bytesRead;

//                // Report progress
//                if (canReportProgress)
//                {
//                    var progress = (int)((totalRead * 100L) / totalBytes);
//                    if (progress != lastProgress)
//                    {
//                        ProgressChanged?.Invoke(progress);
//                        lastProgress = progress;
//                    }
//                }
//                else
//                {
//                    // If content length is unknown, emit coarse progress every ~1 MiB
//                    const int stepBytes = 1024 * 1024;
//                    var progress = (int)(totalRead / stepBytes);
//                    if (progress != lastProgress)
//                    {
//                        ProgressChanged?.Invoke(progress); // “progress” is pseudo-steps, still useful for UI tick
//                        lastProgress = progress;
//                    }
//                }
//            }
//        }, ct).ConfigureAwait(false);

//        // Ensure directory still exists (in case of external moves) then atomically move into place
//        Directory.CreateDirectory(destDir);

//        await WithRetriesAsync(async () =>
//        {
//            // Replace/overwrite if file already exists
//            File.Move(tempPath, finalFullPath, overwrite: true);
//            await Task.CompletedTask;
//        }, ct).ConfigureAwait(false);

//        // Guarantee we signal 100% on success
//        ProgressChanged?.Invoke(100);
//        DownloadCompleted?.Invoke(finalFullPath);
//    }

//    private static void TryDeleteFileIfExists(string path)
//    {
//        try
//        {
//            if (File.Exists(path)) File.Delete(path);
//        }
//        catch
//        {
//            // best-effort cleanup; ignore
//        }
//    }

//    /// <summary>
//    /// Simple linear backoff with small jitter to handle transient file sharing/IO issues.
//    /// </summary>
//    private static async Task WithRetriesAsync(Func<Task> action, CancellationToken ct, int attempts = 5, int baseDelayMs = 200)
//    {
//        var rnd = new Random();
//        for (int i = 1; i <= attempts; i++)
//        {
//            ct.ThrowIfCancellationRequested();
//            try
//            {
//                await action().ConfigureAwait(false);
//                return;
//            }
//            catch (Exception ex) when (i < attempts && IsTransientFileException(ex))
//            {
//                var delay = baseDelayMs * i + rnd.Next(0, 100); // linear + jitter
//                await Task.Delay(delay, ct).ConfigureAwait(false);
//            }
//        }

//        // Final try without catching to bubble the real error
//        await action().ConfigureAwait(false);
//    }

//    private static bool IsTransientFileException(Exception ex) =>
//        ex is IOException || ex is UnauthorizedAccessException;
//}
