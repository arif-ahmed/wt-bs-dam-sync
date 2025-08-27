using BrandshareDamSync.Abstractions;
using BrandshareDamSync.Domain;
using BrandshareDamSync.Infrastructure.BrandShareDamClient;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.Runtime.CompilerServices;


namespace BrandshareDamSync.Infrastructure.JobExecutors;

public class OneWayDownloadJobExecutor(ILogger<OneWayDownloadJobExecutor> logger, IJobRepository repo, IUnitOfWork uow, IBrandShareDamApi api, IDownloaderService downloader) : IJobExecutorService
{
    public virtual async Task ExecuteJobAsync((string syncId, string tenantId, string jobId) syncJobInfo, CancellationToken ct = default)
    {
        (string syncId, string tenantId, string jobId) = syncJobInfo;

        logger.LogInformation("Job {JobId} started at {Time}", jobId, DateTime.UtcNow);

        var syncJob = await repo.GetByIdAsync(jobId, ct);

        var response = await api.GetFolders(syncJob!.TenantId, jobId);

        if (response.IsSuccessful && response.Content != null)
        {
            var folders = response.Content;

            foreach (var folder in folders)
            {
                try
                {
                    string path = folder.Path?.Replace("\\", "/") ?? string.Empty;
                    string root = syncJob.DestinationPath;

                    string folderPath = folder.Path!;
                    string relativePath = folderPath.TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

                    string localPath = MapToLocalPath(root, relativePath);

                    var foundDirectory = await uow.DirectoryRepository.GetByIdAsync(folder.Id);

                    if (foundDirectory is not null)
                    {
                        var existingPath = MapToLocalPath(root, foundDirectory.Path!.TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));

                        if(!PathsEqual(existingPath, localPath))
                        {
                            Directory.CreateDirectory(Path.GetDirectoryName(localPath)!);
                            Directory.Move(existingPath, localPath);
                        }

                        foundDirectory.LastSeenSyncId = syncId;
                        await uow.DirectoryRepository.UpdateAsync(foundDirectory);
                    }
                    else
                    {
                        var directory = new Folder
                        {
                            Id = folder.Id,
                            ParentId = folder.ParentId,
                            IsActive = folder.IsActive,
                            Label = folder.Label,
                            Path = folder.Path != null ? folder.Path : string.Empty,
                            TenantId = tenantId,
                            CreatedAt = folder.CreatedAt,
                            ModifiedAt = folder.ModifiedAt,
                            LastSeenSyncId = syncId
                        };

                        await uow.DirectoryRepository.AddAsync(directory);
                        await uow.SaveChangesAsync();
                        Directory.CreateDirectory(localPath);
                    }

                }
                catch (IOException ex) 
                {
                    logger.LogError(ex, "I/O operation failed");
                }
                catch (UnauthorizedAccessException ex) 
                {
                    logger.LogError(ex, "UnauthorizedAccessException error");
                }
                catch (Exception ex) 
                {
                    logger.LogError(ex, "error!!");
                } 

            }

            var toDeleteDirectories = await uow.DirectoryRepository.FindAsync(
                predicate: (d => d.LastSeenSyncId != syncId && d.TenantId == tenantId && !d.IsDeleted), 
                cancellationToken: ct);

            foreach (var (id, dirPath) in toDeleteDirectories.Item1.OrderByDescending(p => p.Path.Count(c => c == Path.DirectorySeparatorChar)))
            {
                try
                {
                    if (Directory.Exists(dirPath))
                        Directory.Delete(dirPath, recursive: true);
                }
                catch (IOException) { /* retry/backoff or log */ }
                catch (UnauthorizedAccessException) { /* log / handle */ }
            }


            logger.LogInformation($"Folder's Count: {folders.Count}");
        }

        #region files
        var listItemId = "";
        var modifiedAfterAt = 0; // or ToUnixTimeMilliseconds()

        await foreach (var item in StreamModifiedItems(
                           api, tenantId, jobId, syncJob.VolumeName,
                           isActive: true,
                           lastItemId: listItemId,             // ?? was ""
                           pageSize: 50,
                           modifiedAfterAt: modifiedAfterAt,
                           cancellationToken: ct))
        {
            var (matchDirectories, total ) = await uow.DirectoryRepository.FindAsync(predicate: (d => d.Path == item.FolderPath));
            var nowUtc = DateTime.UtcNow;

            var entity = new FileEntity
            {
                Id = item.ItemId,
                FileName = item.FileName,
                FilePath = item.FilePath,
                FolderPath = item.FolderPath,
                FileId = item.FileId,
                DirectoryId = matchDirectories?.FirstOrDefault()!.Id,
                ModifiedAt = item.ModifiedAt,
                ModifiedAtEpochMs = item.ModifiedAtEpochMs,
                CreatedAtUtc = nowUtc,
                LastModifiedAtUtc = nowUtc,
                TenantId = tenantId,
                LastSeenSyncId = syncId
            };

            var existing = await uow.FileEntityRepository.GetByIdAsync(entity.Id, ct);

            if (existing is not null)
            {
                // ?? CASE A: File already tracked in DB
                // Decide based on path/name
                var destRoot = syncJob.DestinationPath;

                var existingFullPath = BuildFullPath(destRoot, existing.FolderPath, existing.FileName);
                var targetFullPath = BuildFullPath(destRoot, item.FolderPath, item.FileName);

                bool folderSame = string.Equals(
                    Normalise(Path.Combine(destRoot, existing.FolderPath ?? string.Empty)),
                    Normalise(Path.Combine(destRoot, item.FolderPath ?? string.Empty)),
                    FsComparison);

                bool nameSame = string.Equals(
                    existing.FileName ?? string.Empty,
                    item.FileName ?? string.Empty,
                    FsComparison);

                if (folderSame && nameSame)
                {
                    // Same path + name ? re-download to overwrite
                    logger.LogInformation("Same path/name ? re-downloading file.");
                    await DownloadWithLimitsAsync(downloader, item.FilePath, targetFullPath, logger, new DownloadCoordinator(5), ct);
                }
                else if (folderSame && !nameSame)
                {
                    // Same folder, new name ? rename
                    logger.LogInformation("Same folder, file name changed ? renaming local file.");
                    if (File.Exists(existingFullPath))
                        File.Move(existingFullPath, targetFullPath, overwrite: true);
                    else
                        await DownloadWithLimitsAsync(downloader, item.FilePath, targetFullPath, logger, new DownloadCoordinator(5), ct);
                }
                else
                {
                    // Different folder ? move (or download if missing)
                    logger.LogInformation("Folder changed ? moving file.");
                    if (File.Exists(existingFullPath))
                        File.Move(existingFullPath, targetFullPath, overwrite: true);
                    else
                        await DownloadWithLimitsAsync(downloader, item.FilePath, targetFullPath, logger, new DownloadCoordinator(5), ct);
                }

                // update metadata
                existing.FileName = item.FileName;
                existing.FolderPath = item.FolderPath;
                existing.FilePath = item.FilePath;
                existing.FileId = item.FileId;
                existing.DirectoryId = matchDirectories?.FirstOrDefault()?.Id;
                existing.ModifiedAt = item.ModifiedAt;
                existing.ModifiedAtEpochMs = item.ModifiedAtEpochMs;
                existing.LastModifiedAtUtc = DateTime.UtcNow;
                existing.LastSeenSyncId = syncId;

                await uow.SaveChangesAsync(ct);
            }
            else
            {
                // ?? CASE B: File not yet tracked ? treat as new
                var folderPath = entity.FolderPath
                    .Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar)
                    .TrimStart(Path.DirectorySeparatorChar);

                string fullDir = Path.Combine(syncJob.DestinationPath, folderPath);
                string fullPath = Path.Combine(fullDir, entity.FileName);

                fullPath = Path.GetFullPath(fullPath);

                Directory.CreateDirectory(fullDir);

                if (!File.Exists(fullPath))
                {
                    logger.LogInformation("New file detected ? downloading...");
                    await DownloadWithLimitsAsync(downloader, item.FilePath, fullPath, logger, new DownloadCoordinator(5), ct);
                }

                await uow.FileEntityRepository.AddAsync(entity, ct);
                await uow.SaveChangesAsync(ct);
            }

        }

        await foreach (var item in StreamModifiedItems(
                   api, tenantId, jobId, syncJob.VolumeName,
                   isActive: false,
                   lastItemId: listItemId,             // ?? was ""
                   pageSize: 50,
                   modifiedAfterAt: modifiedAfterAt,
                   cancellationToken: ct))
        {
            // 1) Try to find the entity by the (DAM) item id we track as FileEntity.Id
            var existing = await uow.FileEntityRepository.GetByIdAsync(item.ItemId, ct);

            // Helpers
            string Normalise(string p) =>
                Path.GetFullPath(p.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar));
            string BuildFullPath(string root, string folderPath, string fileName)
            {
                var rel = (folderPath ?? string.Empty)
                    .Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar)
                    .TrimStart(Path.DirectorySeparatorChar);
                return Normalise(Path.Combine(root, rel, fileName ?? string.Empty));
            }

            // 2) Decide the local file path to delete
            string? localPath = null;
            if (existing is not null)
            {
                localPath = BuildFullPath(syncJob.DestinationPath, existing.FolderPath, existing.FileName);
            }
            else
            {
                // Defensive fallback if we never tracked it (or row was already removed)
                localPath = BuildFullPath(syncJob.DestinationPath, item.FolderPath, item.FileName);
            }

            // 3) Delete the file if present (ignore if missing)
            try
            {
                if (File.Exists(localPath))
                {
                    File.SetAttributes(localPath, FileAttributes.Normal); // avoid read-only hiccups
                    File.Delete(localPath);
                    logger.LogInformation("??? Deleted local file: {Path}", localPath);
                }
                else
                {
                    logger.LogInformation("Local file already missing: {Path}", localPath);
                }

                // 3a) (Optional) tidy up empty directories up to the sync root
                try
                {
                    var dir = Path.GetDirectoryName(localPath)!;
                    var root = Normalise(syncJob.DestinationPath);
                    while (!string.IsNullOrEmpty(dir) &&
                           Normalise(dir).StartsWith(root, OperatingSystem.IsWindows()
                               ? StringComparison.OrdinalIgnoreCase
                               : StringComparison.Ordinal))
                    {
                        if (Directory.Exists(dir) && !Directory.EnumerateFileSystemEntries(dir).Any())
                            Directory.Delete(dir);
                        else
                            break;
                        dir = Path.GetDirectoryName(dir)!;
                    }
                }
                catch (Exception tidyEx)
                {
                    logger.LogDebug(tidyEx, "Directory cleanup skipped due to error.");
                }
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Failed to delete local file for {ItemId}", item.ItemId);
                // You may choose to continue but still mark DB as deleted; or rethrow to stop the job.
            }

            // 4) Update the database
            if (existing is not null)
            {
                // Soft-delete
                existing.IsDeleted = true;
                existing.LastModifiedAtUtc = DateTime.UtcNow;
                existing.LastSeenSyncId = syncId;

                // Keep original metadata for audit, but you can also clear the path/name if you prefer:
                // existing.FilePath = null; existing.FileName = null; existing.FolderPath = null;

                await uow.SaveChangesAsync(ct);
                logger.LogInformation("DB soft-deleted: {Id}", existing.Id);

                // If you prefer a HARD DELETE instead of soft delete, replace the above with:
                // await uow.FileEntityRepository.DeleteAsync(existing, ct);
                // await uow.SaveChangesAsync(ct);
                // logger.LogInformation("DB hard-deleted: {Id}", existing.Id);
            }
            else
            {
                logger.LogInformation("No DB row for {ItemId}; local delete handled.", item.ItemId);
            }

            logger.LogInformation("Inactive item processed: {ItemId}", item.ItemId);

            logger.LogInformation($"{item}");
        }
        #endregion

        #region delete orphaned files
        // 1) Collect expected files from DB
        // Deconstruct
        var result = await uow.FileEntityRepository
            .FindAsync(f => f.TenantId == tenantId && !f.IsDeleted && f.DirectoryId != null);

        var files = result.Item1;   // IEnumerable<FileEntity>

        // Now files is IEnumerable<FileEntity>
        var expectedFiles = new HashSet<string>(
            files.Select(f => Path.GetFullPath(
                Path.Combine(syncJob.DestinationPath,
                    (f.FolderPath ?? string.Empty)
                        .Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar)
                        .TrimStart(Path.DirectorySeparatorChar),
                    f.FileName))),
            OperatingSystem.IsWindows() ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal);

        // 2) Enumerate actual files
        foreach (var localFile in Directory.EnumerateFiles(syncJob.DestinationPath, "*", SearchOption.AllDirectories))
        {
            var normalised = Path.GetFullPath(localFile);

            if (!expectedFiles.Contains(normalised))
            {
                try
                {
                    File.SetAttributes(normalised, FileAttributes.Normal);
                    File.Delete(normalised);
                    logger.LogInformation("??? Removed orphan file: {Path}", normalised);

                    // Optional: tidy up empty dirs
                    var dir = Path.GetDirectoryName(normalised)!;
                    while (!string.IsNullOrEmpty(dir) &&
                           dir.StartsWith(Path.GetFullPath(syncJob.DestinationPath),
                               OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal))
                    {
                        if (Directory.Exists(dir) && !Directory.EnumerateFileSystemEntries(dir).Any())
                            Directory.Delete(dir);
                        else
                            break;

                        dir = Path.GetDirectoryName(dir)!;
                    }
                }
                catch (Exception ex)
                {
                    logger.LogWarning(ex, "Failed to delete orphan file: {Path}", normalised);
                }
            }
        }
        #endregion

        logger.LogInformation("Job {JobId} finished at {Time}", syncId, DateTime.UtcNow);
    }

    public async IAsyncEnumerable<ModifiedItem> StreamModifiedItems(
        IBrandShareDamApi api,
        string tenantId,
        string jobId,
        string volumeName,
        bool isActive,
        string? lastItemId,                        // ?? nullable
        int pageSize = 100,
        long? modifiedAfterAt = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var seen = new HashSet<string?>();
        int maxPages = 10_000; // or pass in as a parameter
        int pages = 0;

        // Normalise: empty string behaves like null (param omitted)
        lastItemId = string.IsNullOrWhiteSpace(lastItemId) ? null : lastItemId;

        while (!cancellationToken.IsCancellationRequested)
        {
            var resp = await api.GetModifiedItems(
                tenantId, jobId, volumeName, isActive,
                lastItemId,                         // ?? null => omitted by Refit
                pageSize, modifiedAfterAt,
                cancellationToken);

            if (!resp.IsSuccessStatusCode)
                throw new HttpRequestException($"Failed: {(int)resp.StatusCode} {resp.ReasonPhrase}");

            var items = resp.Content?.Items;
            if (items is null || items.Count == 0)
                yield break;

            if (!seen.Add(lastItemId)) yield break;      // detects cycles > 1
            if (++pages >= maxPages) yield break;        // hard stop, if desired

            // Emit items
            foreach (var item in items)
                yield return item;

            // Advance cursor
            var previousLast = lastItemId;
            lastItemId = items[^1].ItemId;

            // Defensive: if the API returns the same last id again, break to avoid a loop
            if (!string.IsNullOrEmpty(previousLast) && previousLast == lastItemId)
                yield break;
        }
    }


    private static string MapToLocalPath(string root, string damPath)
    {
        // damPath is something like: //APRIL18.../WORLDCUP_GAME_SELECTOR-assets
        var relative = damPath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar);
        // Add any normalisation, invalid char replacement, length handling here
        return Path.Combine(root, relative);
    }

    private static bool PathsEqual(string a, string b) =>
    string.Equals(Path.GetFullPath(a).TrimEnd('\\', '/'),
                  Path.GetFullPath(b).TrimEnd('\\', '/'),
                  OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase
                                              : StringComparison.Ordinal);


    // Build helpers
    static StringComparison FsComparison =>
        OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;

    static string Normalise(string path)
    {
        var p = path.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);
        return Path.GetFullPath(p);
    }

    static string BuildFullPath(string root, string folderPath, string fileName)
    {
        // Ensure folderPath is relative
        var rel = (folderPath ?? string.Empty)
            .Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar)
            .TrimStart(Path.DirectorySeparatorChar);

        var dir = Path.Combine(root, rel);
        Directory.CreateDirectory(dir);
        return Path.GetFullPath(Path.Combine(dir, fileName));
    }

    private async Task DownloadWithLimitsAsync(
        IDownloaderService downloader,
        string sourceUrl,
        string destinationPath,
        ILogger logger,
        DownloadCoordinator coord,
        CancellationToken ct)
    {
        // de-dupe: if the same file target is already being downloaded, skip this one
        if (!coord.InFlightByTargetPath.TryAdd(destinationPath, 0))
        {
            logger.LogInformation("Skipping duplicate in-flight download: {Path}", destinationPath);
            return;
        }

        await coord.Gate.WaitAsync(ct); // throttle
        try
        {
            // simple retry (3 attempts, exponential backoff)
            var delay = TimeSpan.FromSeconds(1);
            for (int attempt = 1; ; attempt++)
            {
                try
                {
                    // Download to temp then replace to avoid partial files
                    var tmp = destinationPath + ".downloading";
                    if (File.Exists(tmp)) File.Delete(tmp);

                    downloader.ProgressChanged += progress =>
                        logger.LogInformation($"Downloading... {progress}%");

                    downloader.DownloadCompleted += filePath =>
                        logger.LogInformation($"? File downloaded to {filePath}");

                    await downloader.DownloadFileAsync(sourceUrl, tmp, ct);

                    // Ensure target directory exists (defensive)
                    Directory.CreateDirectory(Path.GetDirectoryName(destinationPath)!);

                    // Replace atomically when possible
                    if (File.Exists(destinationPath))
                    {
                        // Keep a short-lived backup just in case (auto-cleanup best-effort)
                        var bak = destinationPath + ".bak";
                        try
                        {
                            if (File.Exists(bak)) File.Delete(bak);
                            File.Replace(tmp, destinationPath, bak, ignoreMetadataErrors: true);
                            try { File.Delete(bak); } catch { /* ignore */ }
                        }
                        catch
                        {
                            // Fallback to overwrite move
                            File.Move(tmp, destinationPath, overwrite: true);
                        }
                    }
                    else
                    {
                        File.Move(tmp, destinationPath);
                    }


                    break; // success
                }
                catch (OperationCanceledException) { throw; }
                catch (Exception ex) when (attempt < 3)
                {
                    logger.LogWarning(ex, "Download failed (attempt {Attempt}) for {Path}. Retrying in {Delay}…",
                                      attempt, destinationPath, delay);
                    await Task.Delay(delay, ct);
                    delay = TimeSpan.FromSeconds(delay.TotalSeconds * 2);
                }
            }
        }
        finally
        {
            coord.Gate.Release();
            coord.InFlightByTargetPath.TryRemove(destinationPath, out _);
        }
    }


}

public sealed class DownloadCoordinator
{
    public SemaphoreSlim Gate { get; }
    public ConcurrentDictionary<string, byte> InFlightByTargetPath { get; } = new();

    public DownloadCoordinator(int maxConcurrent) =>
        Gate = new SemaphoreSlim(maxConcurrent, maxConcurrent);
}
