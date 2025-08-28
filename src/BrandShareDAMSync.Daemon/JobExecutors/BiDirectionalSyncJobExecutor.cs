﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿using BrandshareDamSync.Abstractions;
using BrandshareDamSync.Domain;
using BrandshareDamSync.Infrastructure.BrandShareDamClient;
using BrandshareDamSync.Infrastructure.Utils;
using BrandshareDamSync.Infrastructure.S3;
using Microsoft.EntityFrameworkCore;
using AutoMapper;
using Amazon.S3;
using Refit;
using System.Runtime.CompilerServices;

namespace BrandshareDamSync.Daemon.JobExecutors;

public class BiDirectionalSyncJobExecutor(ILogger<BiDirectionalSyncJobExecutor> logger, IBrandShareDamApi api, IUnitOfWork uow, IDownloaderService downloader, IMapper mapper) : IJobExecutorService
{
    public async Task ExecuteJobAsync((string syncId, string tenantId, string jobId) syncJobInfo, CancellationToken ct = default)
    {
        (string syncId, string tenantId, string jobId) = syncJobInfo;

        var syncJob = await uow.JobRepository.GetByIdAsync(jobId, ct);

        if (syncJob == null)
        {
            logger.LogWarning("Job: {jobId} not found", jobId);
            return;
        }

        if (!syncJob.IsActive)
        {
            logger.LogInformation("Job: {jobId} is not active, skipping", jobId);
            return;
        }

        var getDamFoldersApiResponse = await api.GetFolders(syncJob!.TenantId, jobId);

        if (getDamFoldersApiResponse != null && getDamFoldersApiResponse.IsSuccessful) 
        {
            var damFolders = getDamFoldersApiResponse.Content;

            foreach (var folder in damFolders) 
            {
                string basePath = syncJob.DestinationPath ?? string.Empty;
                string absolutePath = Path.Combine(basePath, folder.Path.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));

                string relativePath = Path.GetRelativePath(basePath, absolutePath).Replace("\\", "/");

                logger.LogInformation("Job: {jobId} - Found DAM Folder: {folderId} - {folderName}", jobId, folder.Id, folder.Label);

                if (Path.Exists(absolutePath)) 
                {
                    logger.LogInformation("Job: {jobId} - Local folder exists: {localFolderPath}", jobId, absolutePath);
                } 
                else 
                {
                    try 
                    {
                        Directory.CreateDirectory(absolutePath);
                        logger.LogInformation("Job: {jobId} - Created local folder: {localFolderPath}", jobId, absolutePath);
                    } 
                    catch (Exception ex) 
                    {
                        logger.LogError(ex, "Job: {jobId} - Failed to create local folder: {localFolderPath}", jobId, absolutePath);
                    }
                }

                var dbRef = await uow.DirectoryRepository.GetByIdAsync(folder.Id, ct);

                if(dbRef == null) 
                {
                    dbRef = new Folder
                    {
                        Id = folder.Id,
                        ParentId = folder.ParentId,
                        TenantId = syncJob.TenantId,
                        Path = relativePath, // folder.Path,
                        Label = folder.Label,
                        CreatedAt = folder.CreatedAt,
                        ModifiedAt = folder.ModifiedAt,
                        LastSeenSyncId = syncId

                    };

                    await uow.DirectoryRepository.AddAsync(dbRef, ct);
                } 
                else 
                {
                    dbRef.Id = folder.Id;
                    dbRef.Label = folder.Label;
                    dbRef.ParentId = folder.ParentId;
                    dbRef.Path = relativePath;
                    dbRef.CreatedAt = folder.CreatedAt;
                    dbRef.ModifiedAt = folder.ModifiedAt;
                    dbRef.LastSeenSyncId = syncId;
                    await uow.DirectoryRepository.UpdateAsync(dbRef, ct);
                }

                // create local folder path from dam folder path
                // check if folder exists in local file system
                // if not, create it
                // if exists, log and continue
                // update db record with last synced timestamp
            }

            await uow.SaveChangesAsync(ct);

            var dbDirectoriesRef = await uow.DirectoryRepository.Query().Where(d => d.TenantId == tenantId).ToListAsync();

            // === Page through DAM "modified items" and process them ===
            // If you already track a cursor (last processed DAM item id), load it here.
            // Add a string? property (e.g., LastDamCursor) on SyncJob if you haven't yet.
            long? lastRunTime = null;
            var pageSize = 200;
            string? cursor = syncJob.LastItemId; // TODO: ensure this property exists on your SyncJob

            logger.LogInformation("Job: {jobId} - Streaming modified items from DAM (startCursor={cursor}, pageSize={pageSize})",
                jobId, cursor ?? "<null>", pageSize);

            await foreach (var item in api.StreamModifiedItems(
                tenantId: syncJob.TenantId,
                jobId: jobId,
                volumeName: syncJob.VolumeName ?? "default",
                isActive: true,
                lastItemId: cursor,
                pageSize: pageSize,
                modifiedAfterAt: syncJob.LastRunTime,
                onLastRunTime: v => lastRunTime = v,  // <- capture it once
                ct: ct))
            {
                logger.LogDebug("Job: {jobId} - Handling item {itemId} ({type})", jobId, item.ItemId, item.FileName);
                cursor = item.ItemId;

                // Map the API item to FileEntity using AutoMapper
                var fileEntity = mapper.Map<FileEntity>(item);
                
                // Set properties that require manual assignment
                fileEntity.TenantId = syncJob.TenantId;
                fileEntity.LastSeenSyncId = syncId;
                
                // Try to find the corresponding directory for this file
                var correspondingFolder = dbDirectoriesRef
                    .FirstOrDefault(d => item.FolderPath.StartsWith(d.Path, StringComparison.OrdinalIgnoreCase));
                
                if (correspondingFolder != null)
                {
                    fileEntity.DirectoryId = correspondingFolder.Id;
                }
                
                // Check if the file entity already exists in the database
                var existingFileEntity = await uow.FileEntityRepository.GetByIdAsync(item.ItemId, ct);
                
                if (existingFileEntity == null)
                {
                    // Add new file entity
                    await uow.FileEntityRepository.AddAsync(fileEntity, ct);
                    logger.LogInformation("Job: {jobId} - Added new file entity: {fileName} (ID: {itemId})", 
                        jobId, item.FileName, item.ItemId);
                }
                else
                {
                    // Update existing file entity with new data
                    mapper.Map(item, existingFileEntity);
                    existingFileEntity.TenantId = syncJob.TenantId;
                    existingFileEntity.LastSeenSyncId = syncId;
                    existingFileEntity.LastModifiedAtUtc = DateTimeOffset.UtcNow;
                    
                    if (correspondingFolder != null)
                    {
                        existingFileEntity.DirectoryId = correspondingFolder.Id;
                    }
                    
                    await uow.FileEntityRepository.UpdateAsync(existingFileEntity, ct);
                    logger.LogInformation("Job: {jobId} - Updated existing file entity: {fileName} (ID: {itemId})", 
                        jobId, item.FileName, item.ItemId);
                }

                //// Build the local path
                //var filePath = PathUtility.BuildLocalPath(syncJob.DestinationPath!, item.FolderPath, item.FileName);

                //// Subscribe to progress + completion events
                //void OnProgress(int pct) =>
                //    logger.LogInformation("Downloading {file} … {pct}%", filePath, pct);

                //void OnCompleted(string tmpPath) =>
                //    logger.LogInformation("Download complete for {file} (temp: {tmp})", filePath, tmpPath);

                //downloader.ProgressChanged += OnProgress;
                //downloader.DownloadCompleted += OnCompleted;

                //try
                //{
                //    // Perform the download
                //    await downloader.DownloadFileAsync(item.FilePath, filePath, ct);
                //}
                //finally
                //{
                //    // Always unsubscribe to avoid memory leaks
                //    downloader.ProgressChanged -= OnProgress;
                //    downloader.DownloadCompleted -= OnCompleted;
                //}

            }

            // Save all file entity changes to the database
            await uow.SaveChangesAsync(ct);
            logger.LogInformation("Job: {jobId} - Saved all file entity changes to database", jobId);

            // Persist the latest cursor so next run continues where we left off
            if (cursor != syncJob.LastItemId)
            {
                syncJob.LastItemId = cursor;
                syncJob.LastRunTime = lastRunTime ?? DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                await uow.JobRepository.UpdateAsync(syncJob, ct);
                await uow.SaveChangesAsync(ct);
                logger.LogInformation("Job: {jobId} - Updated DAM paging cursor to {cursor}", jobId, cursor ?? "<null>");
            }



            #region delete logics
            dbDirectoriesRef.ExceptBy(damFolders.Select(df => df.Id), d => d.Id)
                .ToList()
                .ForEach(async d => 
                {
                    // delete local folder?
                    // update db record to inactive

                    logger.LogInformation("Job: {jobId} - Deactivating local folder no longer in DAM: {folderId} - {folderPath}", jobId, d.Id, d.Path);
                    d.IsActive = false;
                    await uow.DirectoryRepository.UpdateAsync(d, ct);
                });
            #endregion 

            // =====================================================
            // LOCAL → DAM SYNC PORTION
            // =====================================================
            
            logger.LogInformation("Job: {jobId} - Starting Local to DAM sync", jobId);
            
            await SyncLocalChangesToDAM(syncJob, syncId, tenantId, jobId, dbDirectoriesRef, ct);
        }

        logger.LogInformation("Job: {jobId} - Completed Bi-Directional Sync", jobId);
    }

    /// <summary>
    /// Scans local directory structure and syncs changes to DAM
    /// </summary>
    private async Task SyncLocalChangesToDAM(
        SyncJob syncJob, 
        string syncId, 
        string tenantId, 
        string jobId,
        List<Folder> dbDirectories,
        CancellationToken ct)
    {
        var destinationPath = syncJob.DestinationPath;
        if (string.IsNullOrEmpty(destinationPath) || !Directory.Exists(destinationPath))
        {
            logger.LogWarning("Job: {jobId} - Destination path not found or empty: {path}", jobId, destinationPath);
            return;
        }

        logger.LogInformation("Job: {jobId} - Scanning local directory: {path}", jobId, destinationPath);

        // Enumeration options for directory scanning
        var enumerationOptions = new EnumerationOptions
        {
            RecurseSubdirectories = true,
            IgnoreInaccessible = true,
            AttributesToSkip = FileAttributes.Hidden | FileAttributes.System | FileAttributes.Temporary
        };

        var processedFiles = new HashSet<string>();
        var scannedDirectories = new HashSet<string>();

        try
        {
            // ===== STEP 1: Process Local Directories =====
            await ProcessLocalDirectories(destinationPath, syncJob, syncId, tenantId, jobId, dbDirectories, enumerationOptions, scannedDirectories, ct);

            // ===== STEP 2: Process Local Files =====
            await ProcessLocalFiles(destinationPath, syncJob, syncId, tenantId, jobId, dbDirectories, enumerationOptions, processedFiles, ct);

            // ===== STEP 3: Handle Deleted Items (files not seen in this sync) =====
            await ProcessDeletedFiles(syncId, tenantId, jobId, processedFiles, syncJob, ct);

            // ===== STEP 4: Handle Deleted Directories (directories not seen in this sync) =====
            await ProcessDeletedDirectories(syncId, tenantId, jobId, scannedDirectories, syncJob, ct);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Job: {jobId} - Error during local to DAM sync", jobId);
            throw;
        }
    }

    /// <summary>
    /// Processes local directories and ensures they exist in DAM
    /// </summary>
    private async Task ProcessLocalDirectories(
        string destinationPath, 
        SyncJob syncJob, 
        string syncId, 
        string tenantId, 
        string jobId,
        List<Folder> dbDirectories,
        EnumerationOptions enumerationOptions,
        HashSet<string> scannedDirectories,
        CancellationToken ct)
    {
        var processedCount = 0;
        var skippedCount = 0;
        
        foreach (var directory in Directory.EnumerateDirectories(destinationPath, "*", enumerationOptions))
        {
            ct.ThrowIfCancellationRequested();

            try
            {
                var relativePath = Path.GetRelativePath(destinationPath, directory).Replace("\\", "/");
                // var relativePath = PathStandardizer.CreateStandardizedRelativePath(destinationPath, directory);
                var label = Path.GetFileName(directory);
                
                scannedDirectories.Add(relativePath);
                
                // Check if directory exists in our database first
                var existingDbFolder = await uow.DirectoryRepository.Query()
                    .Where(d => d.TenantId == tenantId && d.Path == relativePath)
                    .FirstOrDefaultAsync(ct);

                if (existingDbFolder != null)
                {
                    // PERFORMANCE OPTIMIZATION: Skip if already processed in current sync
                    if (existingDbFolder.LastSeenSyncId == syncId)
                    {
                        skippedCount++;
                        logger.LogDebug("Job: {jobId} - Directory already processed in current sync, skipping: {path}", jobId, relativePath);
                        continue;
                    }
                    
                    // Directory already tracked, just update sync ID
                    existingDbFolder.LastSeenSyncId = syncId;
                    await uow.DirectoryRepository.UpdateAsync(existingDbFolder, ct);
                    processedCount++;
                    logger.LogDebug("Job: {jobId} - Updated existing directory tracking: {path}", jobId, relativePath);
                }
                else
                {
                    // Check if directory exists in DAM before attempting to create
                    string volumePath = syncJob.VolumePath?.TrimEnd('/') ?? "";
                    string damPath = Path.Combine(volumePath, relativePath).Replace("\\", "/");
                    // string damPath = volumePath + relativePath;
                    var checkFolderResponse = await api.CheckFolderExists(tenantId, jobId, damPath);
                    
                    if (checkFolderResponse?.IsSuccessful == true && checkFolderResponse.Content?.Success == true)
                    {
                        // Folder exists in DAM, create local tracking
                        var folder = new Folder
                        {
                            Id = checkFolderResponse.Content.FolderId,
                            IsActive = true,
                            Label = label,
                            Path = relativePath,
                            TenantId = tenantId,
                            CreatedAt = DateTimeOffset.UtcNow,
                            ModifiedAt = DateTimeOffset.UtcNow,
                            CreatedAtUtc = DateTimeOffset.UtcNow,
                            LastModifiedAtUtc = DateTimeOffset.UtcNow,
                            LastSeenSyncId = syncId
                        };
                        
                        await uow.DirectoryRepository.AddAsync(folder, ct);
                        processedCount++;
                        logger.LogInformation("Job: {jobId} - Added tracking for existing DAM directory: {path}", jobId, relativePath);
                    }
                    else
                    {
                        // Directory doesn't exist in DAM, need to create it
                        await CreateDirectoryInDAM(directory, relativePath, label, syncJob, syncId, tenantId, jobId, ct);
                        processedCount++;
                    }
                }
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Job: {jobId} - Error processing directory: {directory}", jobId, directory);
            }
        }
        
        await uow.SaveChangesAsync(ct);
        logger.LogInformation("Job: {jobId} - Directory processing completed. Processed: {processed}, Skipped: {skipped} (Performance optimization)", 
            jobId, processedCount, skippedCount);
    }

    /// <summary>
    /// Processes local files and syncs them to DAM based on actual filesystem changes
    /// </summary>
    private async Task ProcessLocalFiles(
        string destinationPath, 
        SyncJob syncJob, 
        string syncId, 
        string tenantId, 
        string jobId,
        List<Folder> dbDirectories,
        EnumerationOptions enumerationOptions,
        HashSet<string> processedFiles,
        CancellationToken ct)
    {
        var totalFilesProcessed = 0;
        var filesSkipped = 0;
        var newFiles = 0;
        var modifiedFiles = 0;
        var unchangedFiles = 0;
        
        foreach (var filePath in Directory.EnumerateFiles(destinationPath, "*", enumerationOptions))
        {
            ct.ThrowIfCancellationRequested();

            try
            {
                totalFilesProcessed++;
                var fileInfo = new FileInfo(filePath);
                var relativePath = PathStandardizer.CreateStandardizedRelativePath(destinationPath, Path.GetDirectoryName(filePath) ?? destinationPath);
                var fileName = fileInfo.Name;
                var currentFileHash = await FileUtils.Sha256Async(filePath);
                var lastModifiedUtc = fileInfo.LastWriteTimeUtc;
                
                processedFiles.Add(Path.Combine(relativePath, fileName));

                logger.LogDebug("Job: {jobId} - Processing local file: {file} (Modified: {lastModified})", 
                    jobId, fileName, lastModifiedUtc);

                // Check if we have local tracking for this file
                var dbRef = await uow.FileEntityRepository.Query()
                    .Where(f => f.TenantId == tenantId && f.FolderPath == relativePath && f.FileName == fileName)
                    .FirstOrDefaultAsync(ct);

                if (dbRef == null)
                {
                    // NEW FILE: Not in database - this is a new local file to upload
                    newFiles++;
                    logger.LogInformation("Job: {jobId} - New local file detected: {fileName}", jobId, fileName);
                    await HandleNewLocalFile(filePath, relativePath, fileName, currentFileHash, fileInfo, 
                        syncJob, syncId, tenantId, jobId, ct);
                }
                else
                {
                    // PERFORMANCE OPTIMIZATION: Skip if already processed in current sync
                    if (dbRef.LastSeenSyncId == syncId)
                    {
                        filesSkipped++;
                        logger.LogDebug("Job: {jobId} - File already processed in current sync, skipping: {fileName}", jobId, fileName);
                        processedFiles.Add(Path.Combine(relativePath, fileName)); // Still track as processed
                        continue;
                    }
                    
                    // EXISTING FILE: Check if it has changed since last sync
                    bool hasContentChanged = dbRef.ChecksumHash != currentFileHash;
                    bool hasTimestampChanged = dbRef.LastModifiedAtUtc < lastModifiedUtc;
                    
                    if (hasContentChanged || hasTimestampChanged)
                    {
                        // MODIFIED FILE: File has changed since last sync
                        modifiedFiles++;
                        logger.LogInformation("Job: {jobId} - Modified local file detected: {fileName} (Content: {contentChanged}, Timestamp: {timestampChanged})", 
                            jobId, fileName, hasContentChanged, hasTimestampChanged);
                        await HandleModifiedLocalFile(dbRef, filePath, relativePath, fileName, currentFileHash, fileInfo, 
                            syncJob, syncId, tenantId, jobId, ct);
                    }
                    else
                    {
                        // UNCHANGED FILE: Just update sync tracking
                        unchangedFiles++;
                        logger.LogDebug("Job: {jobId} - File unchanged: {fileName}", jobId, fileName);
                        dbRef.LastSeenSyncId = syncId;
                        // Ensure checksum is up to date for future change detection
                        dbRef.ChecksumHash = currentFileHash;
                        dbRef.LastModifiedAtUtc = lastModifiedUtc;
                        await uow.FileEntityRepository.UpdateAsync(dbRef, ct);
                        // Save changes immediately to ensure syncId and checksum tracking are persisted
                        await uow.SaveChangesAsync(ct);
                    }
                }
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Job: {jobId} - Error processing file: {file}", jobId, filePath);
            }
        }

        // All changes are saved immediately in individual operations above
        logger.LogInformation("Job: {jobId} - File processing completed. Total: {total}, New: {new}, Modified: {modified}, Unchanged: {unchanged}, Skipped: {skipped} (Performance optimization)", 
            jobId, totalFilesProcessed, newFiles, modifiedFiles, unchangedFiles, filesSkipped);
    }

    /// <summary>
    /// Creates a new directory in DAM following OneWayUploadJobExecutor patterns
    /// </summary>
    private async Task CreateDirectoryInDAM(
        string absolutePath,
        string relativePath,
        string label,
        SyncJob syncJob, 
        string syncId, 
        string tenantId, 
        string jobId,
        CancellationToken ct)
    {
        try
        {
            // Get parent directory path using PathStandardizer
            string parentPath = Path.GetDirectoryName(absolutePath) ?? string.Empty;
            string destinationPath = syncJob.DestinationPath ?? string.Empty;
            
            // Convert to DAM path format
            string damParentPath;
            if (parentPath == destinationPath)
            {
                // Parent is the root directory
                damParentPath = syncJob.VolumePath?.TrimEnd('/') ?? "";
            }
            else
            {
                // Get standardized relative path and combine with volume path
                string standardizedParentPath = PathStandardizer.CreateStandardizedRelativePath(destinationPath, parentPath);
                damParentPath = (syncJob.VolumePath?.TrimEnd('/') ?? "") + standardizedParentPath;
            }
            
            // Check if parent folder exists in DAM
            ApiResponse<BoolSuccessResponse>? parentCheckResponse = null;
            string parentId = syncJob.VolumeId;
            
            if (string.IsNullOrEmpty(damParentPath) || damParentPath == syncJob.VolumePath?.TrimEnd('/'))
            {
                // This is a root level folder - use empty parent ID
                parentId = syncJob.VolumeId;
                // Skip parent check for root level
            }
            else
            {
                parentCheckResponse = await api.CheckFolderExists(tenantId, jobId, damParentPath);
                
                if (parentCheckResponse?.IsSuccessStatusCode == true && parentCheckResponse.Content?.Success == true)
                {
                    parentId = parentCheckResponse.Content.FolderId;
                }
                else
                {
                    logger.LogError("Job: {jobId} - Parent folder does not exist in DAM: {parentPath}", jobId, damParentPath);
                    return;
                }
            }
            
            // Create the folder in DAM
            logger.LogInformation("Job: {jobId} - Creating new folder in DAM: {path}", jobId, relativePath);
            
            var createFolderResponse = await api.CreateFolder(tenantId, jobId, parentId, label);
                
                if (createFolderResponse?.IsSuccessful == true && createFolderResponse.Content != null)
                {
                    var folder = new Folder
                    {
                        Id = createFolderResponse.Content.Id,
                        IsActive = true,
                        ParentId = parentId,
                        Label = label,
                        Path = relativePath,
                        TenantId = tenantId,
                        CreatedAt = DateTimeOffset.UtcNow,
                        ModifiedAt = DateTimeOffset.UtcNow,
                        CreatedAtUtc = DateTimeOffset.UtcNow,
                        LastModifiedAtUtc = DateTimeOffset.UtcNow,
                        LastSeenSyncId = syncId
                    };

                    await uow.DirectoryRepository.AddAsync(folder, ct);
                    await uow.SaveChangesAsync(ct); // Ensure new directory tracking is immediately persisted
                    logger.LogInformation("Job: {jobId} - Successfully created directory in DAM: {path} (ID: {folderId})", 
                        jobId, relativePath, createFolderResponse.Content.Id);
                }
                else
                {
                    logger.LogError("Job: {jobId} - Failed to create folder in DAM: {path}", jobId, relativePath);
                }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Job: {jobId} - Error creating directory in DAM: {path}", jobId, relativePath);
        }
    }

    /// <summary>
    /// Handles a new local file that needs to be uploaded to DAM
    /// </summary>
    private async Task HandleNewLocalFile(
        string filePath,
        string relativePath,
        string fileName,
        string fileHash,
        FileInfo fileInfo,
        SyncJob syncJob,
        string syncId,
        string tenantId,
        string jobId,
        CancellationToken ct)
    {
        try
        {
            // Convert paths to DAM format
            string volumePath = syncJob.VolumePath?.TrimEnd('/') ?? "";
            string damDirectoryPath = relativePath;
            string damFilePath = volumePath + damDirectoryPath + "/" + fileName;
            
            // Check if file already exists in DAM (might be uploaded by another sync)
            var checkFileResponse = await api.CheckFileExists(tenantId, jobId, damFilePath);
            
            if (checkFileResponse?.IsSuccessStatusCode == true && checkFileResponse.Content?.Success == true)
            {
                // File exists in DAM but not in our local tracking - create tracking entry
                logger.LogInformation("Job: {jobId} - File exists in DAM but not tracked locally: {fileName}", jobId, fileName);
                await CreateLocalFileTracking(checkFileResponse.Content.ItemId, filePath, relativePath, fileName, 
                    fileHash, fileInfo, syncJob, syncId, tenantId, ct);
                // Save changes immediately to ensure tracking is persisted
                await uow.SaveChangesAsync(ct);
            }
            else
            {
                // File doesn't exist in DAM - upload it
                logger.LogInformation("Job: {jobId} - Uploading new file to DAM: {fileName}", jobId, fileName);
                await UploadNewFileToDAM(filePath, relativePath, fileName, fileHash, 
                    syncJob, syncId, tenantId, jobId, ct);
                // Save changes immediately to ensure upload tracking is persisted
                await uow.SaveChangesAsync(ct);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Job: {jobId} - Error handling new local file: {fileName}", jobId, fileName);
        }
    }

    /// <summary>
    /// Handles a modified local file that needs to be updated in DAM
    /// </summary>
    private async Task HandleModifiedLocalFile(
        FileEntity dbRef,
        string filePath,
        string relativePath,
        string fileName,
        string currentFileHash,
        FileInfo fileInfo,
        SyncJob syncJob,
        string syncId,
        string tenantId,
        string jobId,
        CancellationToken ct)
    {
        try
        {
            // Check if file still exists in DAM
            string volumePath = syncJob.VolumePath?.TrimEnd('/') ?? "";
            string damFilePath = volumePath + relativePath + "/" + fileName;
            
            var checkFileResponse = await api.CheckFileExists(tenantId, jobId, damFilePath);
            
            if (checkFileResponse?.IsSuccessStatusCode == true && checkFileResponse.Content?.Success == true)
            {
                // File exists in DAM - update it
                logger.LogInformation("Job: {jobId} - Updating modified file in DAM: {fileName}", jobId, fileName);
                await ReUploadChangedFile(checkFileResponse.Content.ItemId, filePath, currentFileHash, 
                    dbRef, syncJob, syncId, tenantId, ct);
                
                // Update local tracking with new file info (checksum should be updated in ReUploadChangedFile)
                // But ensure it's updated here as well for consistency
                dbRef.ChecksumHash = currentFileHash;
                dbRef.SizeInBytes = fileInfo.Length;
                dbRef.LastModifiedAtUtc = fileInfo.LastWriteTimeUtc;
                dbRef.LastSeenSyncId = syncId;
                await uow.FileEntityRepository.UpdateAsync(dbRef, ct);
                // Save changes immediately to ensure modifications are persisted
                await uow.SaveChangesAsync(ct);
            }
            else
            {
                // File no longer exists in DAM - treat as new file
                logger.LogWarning("Job: {jobId} - File no longer exists in DAM, re-uploading: {fileName}", jobId, fileName);
                await UploadNewFileToDAM(filePath, relativePath, fileName, currentFileHash, 
                    syncJob, syncId, tenantId, jobId, ct);
                
                // Remove old tracking since we're creating new
                await uow.FileEntityRepository.DeleteAsync(dbRef.Id, ct);
                // Save changes immediately to ensure old tracking is removed and new tracking is persisted
                await uow.SaveChangesAsync(ct);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Job: {jobId} - Error handling modified local file: {fileName}", jobId, fileName);
        }
    }

    /// <summary>
    /// Creates local tracking for a file that exists in DAM but wasn't tracked locally.
    /// IMPORTANT: This method includes duplicate ID prevention logic to handle race conditions
    /// in bi-directional sync where the same file might be processed in both DAM→Local and Local→DAM flows.
    /// </summary>
    private async Task CreateLocalFileTracking(
        string itemId,
        string filePath,
        string relativePath,
        string fileName,
        string fileHash,
        FileInfo fileInfo,
        SyncJob syncJob,
        string syncId,
        string tenantId,
        CancellationToken ct)
    {
        try
        {
            // Check if FileEntity with this ID already exists (could have been created during DAM → Local sync)
            var existingEntity = await uow.FileEntityRepository.GetByIdAsync(itemId, ct);
            if (existingEntity != null)
            {
                // Entity already exists, just update the tracking info
                existingEntity.LastSeenSyncId = syncId;
                existingEntity.ChecksumHash = fileHash;
                existingEntity.SizeInBytes = fileInfo.Length;
                existingEntity.LastModifiedAtUtc = fileInfo.LastWriteTimeUtc;
                
                await uow.FileEntityRepository.UpdateAsync(existingEntity, ct);
                await uow.SaveChangesAsync(ct);
                logger.LogInformation("Job: {jobId} - Updated existing tracking for DAM file: {fileName} (ID: {itemId})", 
                    syncJob.Id, fileName, itemId);
                return;
            }

            var fileEntity = new FileEntity
            {
                Id = itemId,
                TenantId = tenantId,
                FolderPath = relativePath,
                FileName = fileName,
                ChecksumHash = fileHash,
                SizeInBytes = fileInfo.Length,
                CreatedAtUtc = fileInfo.CreationTimeUtc,
                LastModifiedAtUtc = fileInfo.LastWriteTimeUtc,
                LastSeenSyncId = syncId
            };

            await uow.FileEntityRepository.AddAsync(fileEntity, ct);
            await uow.SaveChangesAsync(ct); // Ensure tracking is immediately persisted
            logger.LogInformation("Job: {jobId} - Created local tracking for existing DAM file: {fileName} (ID: {itemId})", 
                syncJob.Id, fileName, itemId);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Job: {jobId} - Error creating local file tracking: {fileName}", syncJob.Id, fileName);
        }
    }



    /// <summary>
    /// Re-uploads a changed file using OneWayUploadJobExecutor patterns
    /// </summary>
    private async Task ReUploadChangedFile(
        string itemId,
        string filePath,
        string newChecksum,
        FileEntity dbRef,
        SyncJob syncJob,
        string syncId,
        string tenantId,
        CancellationToken ct)
    {
        try
        {
            // Get replacement upload details
            var replacementDetails = await api.GetUploadDetailsForFileReplacement(tenantId, itemId, ct);
            if (!replacementDetails.IsSuccessStatusCode || replacementDetails.Content == null)
            {
                logger.LogError("Job: {jobId} - Failed to get replacement upload details for: {itemId}", syncJob.Id, itemId);
                return;
            }

            // Get S3 info and create uploader
            var s3InfoResponse = await api.GetS3Info(tenantId, ct);
            if (!s3InfoResponse.IsSuccessStatusCode || s3InfoResponse.Content == null)
            {
                logger.LogError("Job: {jobId} - Failed to get S3 info for replacement", syncJob.Id);
                return;
            }

            var s3Info = s3InfoResponse.Content;
            var s3Client = new AmazonS3Client(s3Info.Key, s3Info.Secret, Amazon.RegionEndpoint.EUWest1);
            var uploader = new S3FileUploader(s3Client, s3Info.Bucket, s3Info.Id);
            
            uploader.Progress += (message) => logger.LogInformation("Job: {jobId} - {message}", syncJob.Id, message);
            
            // Upload the file
            await uploader.UploadAsync(filePath, replacementDetails.Content.Key);
            
            // Replace the asset in DAM
            await api.ReplaceAsset(tenantId, itemId, ct);
            
            // Update database tracking with new checksum and metadata
            dbRef.ChecksumHash = newChecksum;
            dbRef.SizeInBytes = new FileInfo(filePath).Length;
            dbRef.LastSeenSyncId = syncId;
            dbRef.LastModifiedAtUtc = DateTimeOffset.UtcNow;
            
            await uow.FileEntityRepository.UpdateAsync(dbRef, ct);
            await uow.SaveChangesAsync(ct); // Ensure checksum and metadata updates are immediately persisted
            
            logger.LogInformation("Job: {jobId} - Successfully re-uploaded changed file: {fileName} (ID: {itemId})", 
                syncJob.Id, dbRef.FileName, itemId);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Job: {jobId} - Error re-uploading file: {itemId}", syncJob.Id, itemId);
        }
    }

    /// <summary>
    /// Uploads a new file to DAM using OneWayUploadJobExecutor patterns
    /// </summary>
    private async Task UploadNewFileToDAM(
        string filePath,
        string folderPath,
        string fileName,
        string fileHash,
        SyncJob syncJob,
        string syncId,
        string tenantId,
        string jobId,
        CancellationToken ct)
    {
        try
        {
            var fileInfo = new FileInfo(filePath);
            
            // Convert paths to DAM format (following OneWayUploadJobExecutor pattern)
            string damDirectoryPath = PathStandardizer.CreateStandardizedRelativePath(syncJob.DestinationPath ?? "", Path.GetDirectoryName(filePath) ?? "");
            string volumePath = syncJob.VolumePath?.TrimEnd('/') ?? "";
            string damFileDirectoryPath = volumePath + damDirectoryPath;
            
            // Get upload details from DAM (matching OneWayUploadJobExecutor)
            var uploadDetailsResponse = await api.GetUploadDetails(tenantId, jobId, damFileDirectoryPath, fileName);
            if (!uploadDetailsResponse.IsSuccessStatusCode || uploadDetailsResponse.Content == null)
            {
                logger.LogError("Job: {jobId} - Failed to get upload details for: {file}", jobId, fileName);
                return;
            }

            // Get S3 credentials (matching OneWayUploadJobExecutor)
            var s3InfoResponse = await api.GetS3Info(tenantId, ct);
            if (!s3InfoResponse.IsSuccessStatusCode || s3InfoResponse.Content == null)
            {
                logger.LogError("Job: {jobId} - Failed to get S3 info", jobId);
                return;
            }

            var s3Info = s3InfoResponse.Content;
            
            // Create S3 client and uploader (matching OneWayUploadJobExecutor)
            var s3Client = new AmazonS3Client(s3Info.Key, s3Info.Secret, Amazon.RegionEndpoint.EUWest1);
            var uploader = new S3FileUploader(s3Client, s3Info.Bucket, s3Info.Id);
            
            uploader.Progress += (message) => logger.LogInformation("Job: {jobId} - Upload progress: {message}", jobId, message);
            
            // Upload file to S3
            await uploader.UploadAsync(filePath, uploadDetailsResponse.Content.Key);
            
            // Create item in DAM (matching OneWayUploadJobExecutor)
            var damItem = new CreateItemV2Request
            {
                Reference = uploadDetailsResponse.Content.Reference,
                FileSize = fileInfo.Length,
                LoadMetadataFromFile = true
            };
            
            var itemCreateResponse = await api.CreateItemV2(tenantId, damItem);
            if (!itemCreateResponse.IsSuccessStatusCode || itemCreateResponse.Content?.Status != "Success")
            {
                logger.LogError("Job: {jobId} - Failed to create DAM item for: {file}", jobId, fileName);
                return;
            }

            // Create database tracking entry (matching OneWayUploadJobExecutor)
            // IMPORTANT: Check for existing entity to prevent UNIQUE constraint violations
            // in bi-directional sync scenarios where DAM→Local sync may have already created the entity
            var existingEntity = await uow.FileEntityRepository.GetByIdAsync(itemCreateResponse.Content.Id, ct);
            if (existingEntity != null)
            {
                // Entity already exists, update it with local file info
                existingEntity.ChecksumHash = fileHash;
                existingEntity.LastSeenSyncId = syncId;
                existingEntity.SizeInBytes = fileInfo.Length;
                existingEntity.LastModifiedAtUtc = fileInfo.LastWriteTimeUtc;
                existingEntity.FolderPath = folderPath;
                existingEntity.FileName = fileName;
                
                await uow.FileEntityRepository.UpdateAsync(existingEntity, ct);
                await uow.SaveChangesAsync(ct);
                logger.LogInformation("Job: {jobId} - Updated existing tracking for uploaded file: {file} (ID: {itemId})", 
                    jobId, fileName, itemCreateResponse.Content.Id);
            }
            else
            {
                var dbRef = new FileEntity
                {
                    Id = itemCreateResponse.Content.Id,
                    ChecksumHash = fileHash,
                    LastSeenSyncId = syncId,
                    TenantId = tenantId,
                    FolderPath = folderPath,
                    FileName = fileName,
                    SizeInBytes = fileInfo.Length,
                    CreatedAtUtc = DateTimeOffset.UtcNow,
                    LastModifiedAtUtc = fileInfo.LastWriteTimeUtc
                };

                await uow.FileEntityRepository.AddAsync(dbRef, ct);
                await uow.SaveChangesAsync(ct); // Ensure new file tracking is immediately persisted
                logger.LogInformation("Job: {jobId} - Successfully uploaded new file to DAM: {file} (ID: {itemId})", 
                    jobId, fileName, itemCreateResponse.Content.Id);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Job: {jobId} - Error uploading file to DAM: {file}", jobId, fileName);
        }
    }

    /// <summary>
    /// Handles files that were deleted locally by comparing database tracking with actual filesystem.
    /// Uses configurable deletion policy from SyncJob settings.
    /// </summary>
    private async Task ProcessDeletedFiles(
        string syncId,
        string tenantId,
        string jobId,
        HashSet<string> processedFiles,
        SyncJob syncJob,
        CancellationToken ct)
    {
        try
        {
            // Get all files in database for this tenant that were seen in previous syncs
            var allTrackedFiles = await uow.FileEntityRepository.Query()
                .Where(f => f.TenantId == tenantId && !f.IsDeleted)
                .ToListAsync(ct);

            var deletedFiles = new List<FileEntity>();

            foreach (var trackedFile in allTrackedFiles)
            {
                ct.ThrowIfCancellationRequested();
                
                // Create the expected file path for comparison
                var expectedFilePath = Path.Combine(trackedFile.FolderPath ?? "", trackedFile.FileName ?? "");
                
                // If this file wasn't found in current filesystem scan, it's been deleted
                if (!processedFiles.Contains(expectedFilePath))
                {
                    deletedFiles.Add(trackedFile);
                    logger.LogInformation("Job: {jobId} - File deleted locally: {file} (ID: {itemId})", 
                        jobId, trackedFile.FileName, trackedFile.Id);
                }
            }

            if (deletedFiles.Any())
            {
                logger.LogInformation("Job: {jobId} - Found {count} deleted files to process", 
                    jobId, deletedFiles.Count);

                foreach (var deletedFile in deletedFiles)
                {
                    ct.ThrowIfCancellationRequested();
                    
                    // Apply deletion policy from sync job configuration
                    switch (syncJob.FileDeletionPolicy)
                    {
                        case DeletionPolicy.SoftDeleteOnly:
                            // Mark as deleted but keep record - SAFEST OPTION
                            deletedFile.IsDeleted = true;
                            deletedFile.LastModifiedAtUtc = DateTimeOffset.UtcNow;
                            await uow.FileEntityRepository.UpdateAsync(deletedFile, ct);
                            
                            logger.LogInformation("Job: {jobId} - Marked file as soft-deleted: {fileName} (ID: {itemId})", 
                                jobId, deletedFile.FileName, deletedFile.Id);
                            break;

                        case DeletionPolicy.RemoveTrackingOnly:
                            // Remove local tracking entirely but preserve DAM
                            await uow.FileEntityRepository.DeleteAsync(deletedFile.Id, ct);
                            logger.LogInformation("Job: {jobId} - Removed local tracking for deleted file: {fileName} (ID: {itemId})", 
                                jobId, deletedFile.FileName, deletedFile.Id);
                            break;

                        case DeletionPolicy.DeleteFromDAM:
                            // Delete from DAM system as well - WARNING: DATA LOSS POSSIBLE
                            try
                            {
                                var deleteResponse = await api.DeleteItem(tenantId, jobId, deletedFile.Id);
                                if (deleteResponse.IsSuccessStatusCode)
                                {
                                    await uow.FileEntityRepository.DeleteAsync(deletedFile.Id, ct);
                                    logger.LogInformation("Job: {jobId} - Deleted file from DAM and removed tracking: {fileName} (ID: {itemId})", 
                                        jobId, deletedFile.FileName, deletedFile.Id);
                                }
                                else
                                {
                                    logger.LogWarning("Job: {jobId} - Failed to delete file from DAM: {fileName} (ID: {itemId})", 
                                        jobId, deletedFile.FileName, deletedFile.Id);
                                    // Fallback to soft delete
                                    deletedFile.IsDeleted = true;
                                    deletedFile.LastModifiedAtUtc = DateTimeOffset.UtcNow;
                                    await uow.FileEntityRepository.UpdateAsync(deletedFile, ct);
                                }
                            }
                            catch (Exception ex)
                            {
                                logger.LogError(ex, "Job: {jobId} - Error deleting file from DAM: {fileName} (ID: {itemId})", 
                                    jobId, deletedFile.FileName, deletedFile.Id);
                                // Fallback to soft delete
                                deletedFile.IsDeleted = true;
                                deletedFile.LastModifiedAtUtc = DateTimeOffset.UtcNow;
                                await uow.FileEntityRepository.UpdateAsync(deletedFile, ct);
                            }
                            break;

                        case DeletionPolicy.Ignore:
                        default:
                            // No action taken for deleted files
                            logger.LogDebug("Job: {jobId} - Ignoring deleted file per policy: {fileName} (ID: {itemId})", 
                                jobId, deletedFile.FileName, deletedFile.Id);
                            break;
                    }
                }

                await uow.SaveChangesAsync(ct);
                logger.LogInformation("Job: {jobId} - Processed {count} deleted files", 
                    jobId, deletedFiles.Count);
            }
            else
            {
                logger.LogDebug("Job: {jobId} - No deleted files found", jobId);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Job: {jobId} - Error processing deleted files", jobId);
        }
    }

    /// <summary>
    /// Handles directories that were deleted locally by comparing database tracking with actual filesystem
    /// </summary>
    private async Task ProcessDeletedDirectories(
        string syncId,
        string tenantId,
        string jobId,
        HashSet<string> scannedDirectories,
        SyncJob syncJob,
        CancellationToken ct)
    {
        try
        {
            // Get all directories in database for this tenant that were seen in previous syncs
            var allTrackedDirectories = await uow.DirectoryRepository.Query()
                .Where(d => d.TenantId == tenantId && !d.IsDeleted)
                .ToListAsync(ct);

            var deletedDirectories = new List<Folder>();

            foreach (var trackedDirectory in allTrackedDirectories)
            {
                ct.ThrowIfCancellationRequested();
                
                // Skip directories that are marked as inactive (already processed)
                if (!trackedDirectory.IsActive)
                    continue;
                
                // Create the expected directory path for comparison
                var expectedDirPath = trackedDirectory.Path?.TrimStart('/') ?? "";
                
                // If this directory wasn't found in current filesystem scan, it's been deleted
                if (!scannedDirectories.Contains(expectedDirPath))
                {
                    deletedDirectories.Add(trackedDirectory);
                    logger.LogInformation("Job: {jobId} - Directory deleted locally: {dirPath} (ID: {dirId})", 
                        jobId, trackedDirectory.Path, trackedDirectory.Id);
                }
            }

            if (deletedDirectories.Any())
            {
                logger.LogInformation("Job: {jobId} - Found {count} deleted directories to process", 
                    jobId, deletedDirectories.Count);

                foreach (var deletedDirectory in deletedDirectories)
                {
                    ct.ThrowIfCancellationRequested();
                    
                    // Apply deletion policy from sync job configuration
                    switch (syncJob.DirectoryDeletionPolicy)
                    {
                        case DeletionPolicy.SoftDeleteOnly:
                            // Mark as deleted but keep record - SAFEST OPTION
                            deletedDirectory.IsDeleted = true;
                            deletedDirectory.LastModifiedAtUtc = DateTimeOffset.UtcNow;
                            await uow.DirectoryRepository.UpdateAsync(deletedDirectory, ct);
                            
                            logger.LogInformation("Job: {jobId} - Marked directory as soft-deleted: {dirPath} (ID: {dirId})", 
                                jobId, deletedDirectory.Path, deletedDirectory.Id);
                            break;

                        case DeletionPolicy.RemoveTrackingOnly:
                            // Remove local tracking entirely but preserve DAM
                            await uow.DirectoryRepository.DeleteAsync(deletedDirectory.Id, ct);
                            logger.LogInformation("Job: {jobId} - Removed local tracking for deleted directory: {dirPath} (ID: {dirId})", 
                                jobId, deletedDirectory.Path, deletedDirectory.Id);
                            break;

                        case DeletionPolicy.DeleteFromDAM:
                            // Delete from DAM system as well - WARNING: DATA LOSS POSSIBLE
                            try
                            {
                                var deleteResponse = await api.DeleteFolder(tenantId, jobId, deletedDirectory.Id);
                                if (deleteResponse.IsSuccessStatusCode)
                                {
                                    await uow.DirectoryRepository.DeleteAsync(deletedDirectory.Id, ct);
                                    logger.LogInformation("Job: {jobId} - Deleted directory from DAM and removed tracking: {dirPath} (ID: {dirId})", 
                                        jobId, deletedDirectory.Path, deletedDirectory.Id);
                                }
                                else
                                {
                                    logger.LogWarning("Job: {jobId} - Failed to delete directory from DAM: {dirPath} (ID: {dirId})", 
                                        jobId, deletedDirectory.Path, deletedDirectory.Id);
                                    // Fallback to soft delete
                                    deletedDirectory.IsDeleted = true;
                                    deletedDirectory.LastModifiedAtUtc = DateTimeOffset.UtcNow;
                                    await uow.DirectoryRepository.UpdateAsync(deletedDirectory, ct);
                                }
                            }
                            catch (Exception ex)
                            {
                                logger.LogError(ex, "Job: {jobId} - Error deleting directory from DAM: {dirPath} (ID: {dirId})", 
                                    jobId, deletedDirectory.Path, deletedDirectory.Id);
                                // Fallback to soft delete
                                deletedDirectory.IsDeleted = true;
                                deletedDirectory.LastModifiedAtUtc = DateTimeOffset.UtcNow;
                                await uow.DirectoryRepository.UpdateAsync(deletedDirectory, ct);
                            }
                            break;

                        case DeletionPolicy.Ignore:
                        default:
                            // No action taken for deleted directories
                            logger.LogDebug("Job: {jobId} - Ignoring deleted directory per policy: {dirPath} (ID: {dirId})", 
                                jobId, deletedDirectory.Path, deletedDirectory.Id);
                            break;
                    }
                }

                await uow.SaveChangesAsync(ct);
                logger.LogInformation("Job: {jobId} - Processed {count} deleted directories", 
                    jobId, deletedDirectories.Count);
            }
            else
            {
                logger.LogDebug("Job: {jobId} - No deleted directories found", jobId);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Job: {jobId} - Error processing deleted directories", jobId);
        }
    }

    /// <summary>
    /// Converts local file path to DAM path format using PathStandardizer
    /// </summary>
    private static string ConvertLocalPathToDAMPath(string localFilePath, SyncJob syncJob)
    {
        var destinationPath = syncJob.DestinationPath ?? "";
        var volumePath = syncJob.VolumePath?.TrimEnd('/') ?? "";
        
        // Get the directory path and standardize it
        var directoryPath = Path.GetDirectoryName(localFilePath) ?? "";
        var standardizedDirPath = PathStandardizer.CreateStandardizedRelativePath(destinationPath, directoryPath);
        
        // Get the filename
        var fileName = Path.GetFileName(localFilePath);
        
        // Combine volume path + standardized directory path + filename
        return volumePath + standardizedDirPath + "/" + fileName;
    }
}
