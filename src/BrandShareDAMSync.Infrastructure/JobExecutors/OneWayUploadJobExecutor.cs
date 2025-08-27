using Amazon.S3;
using BrandshareDamSync.Abstractions;
using BrandshareDamSync.Domain;
using BrandshareDamSync.Infrastructure.BrandShareDamClient;
using BrandshareDamSync.Infrastructure.MockApi;
using BrandshareDamSync.Infrastructure.S3;
using BrandshareDamSync.Infrastructure.Utils;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Reflection.Emit;

namespace BrandshareDamSync.Infrastructure.JobExecutors;
public class OneWayUploadJobExecutor(ILogger<UploadAndCleanJobExecutor> logger, IUnitOfWork uow, IBrandShareDamApi api) : IJobExecutorService
{
    public async Task ExecuteJobAsync((string syncId, string tenantId, string jobId) syncJobInfo, CancellationToken ct = default)
    {
        Dictionary<string, FolderSummary> folderMapper = new Dictionary<string, FolderSummary>();

        (string syncId, string tenantId, string jobId) = syncJobInfo;

        logger.LogInformation($"Job: {jobId} Started");

        var syncJob = await uow.JobRepository.GetByIdAsync(jobId, ct);

        var response = await api.GetFolders(syncJob!.TenantId, jobId);

        if (response.IsSuccessful && response.Content != null)
        {
            response.Content.ForEach(folder =>
            {
                folderMapper.TryAdd(folder.Path, folder);
            });
        }

        var opts = new EnumerationOptions
        {
            RecurseSubdirectories = true,
            IgnoreInaccessible = true,           // skip permission issues
            AttributesToSkip = FileAttributes.ReparsePoint, // avoid junction loops
            ReturnSpecialDirectories = false,    // skip "." and ".."
            MatchCasing = MatchCasing.CaseInsensitive
        };


        string absPath = syncJob!.DestinationPath;

        logger.LogInformation($"Job: {jobId} Info: {syncJob!.DestinationPath}");

        foreach (var dir in Directory.EnumerateDirectories(absPath, "*", opts))
        {
            ct.ThrowIfCancellationRequested();

            try
            {
                // Work that may fail (e.g., reading ACLs, opening files in this dir)

                var relativePath = PathStandardizer.CreateStandardizedRelativePath(syncJob!.DestinationPath, dir);

                var label = Path.GetFileName(dir);

                var directoryDbRef = await uow.DirectoryRepository.Query().Where(d => d.TenantId == tenantId && d.Path == relativePath).FirstOrDefaultAsync(ct);

                if (directoryDbRef != null && !string.IsNullOrEmpty(directoryDbRef.Id))
                {
                    directoryDbRef.LastSeenSyncId = syncId;

                    folderMapper.TryGetValue("/" + relativePath, out var folderSummary);

                    if (folderSummary == null)
                    {
                        // create directory in dam
                        logger.LogInformation($"Creating new folder in DAM: {relativePath}");
                    }

                    await uow.DirectoryRepository.UpdateAsync(directoryDbRef);
                }
                else
                {
                    folderMapper.TryGetValue("/" + relativePath, out var folderSummary);

                    var folder = new Folder();

                    if (folderSummary != null)
                    {
                        // dam directory exists, update db tracking
                        folder.Id = folderSummary.Id;
                        folder.IsActive = folderSummary.IsActive;
                        folder.ParentId = folderSummary.ParentId;
                        folder.Label = label;
                        folder.Path = relativePath;
                        folder.TenantId = tenantId;
                        folder.CreatedAtUtc = folderSummary.CreatedAt;
                        folder.LastModifiedAtUtc = folderSummary.ModifiedAt;
                        folder.LastSeenSyncId = syncId;
                    }
                    else
                    {

                        string parentPath = (Path.GetDirectoryName(dir) ?? string.Empty).Replace('\\', '/');

                        string destinationPath = syncJob.DestinationPath.Replace('\\', '/');

                        parentPath = parentPath.Replace(destinationPath, syncJob.VolumePath);

                        var apiResonse = await api.CheckFolderExists(tenantId, syncJob.Id, parentPath);

                        if (apiResonse != null && apiResonse.IsSuccessful && apiResonse.Content != null)
                        {
                            // parent folder exists, create new folder in dam
                            string parentId = apiResonse.Content.FolderId;
                            logger.LogInformation($"Creating new folder in DAM: {relativePath}");
                            var createFolderResponse = await api.CreateFolder(tenantId, syncJob.Id, parentId, label);
                            if (createFolderResponse != null && createFolderResponse.IsSuccessful && createFolderResponse.Content != null)
                            {
                                folder.Id = createFolderResponse.Content.Id;
                                folder.IsActive = true;
                                folder.ParentId = parentId;
                                folder.Label = label;
                                folder.Path = relativePath;
                                folder.TenantId = tenantId;
                                folder.CreatedAtUtc = DateTime.UtcNow;
                                folder.LastModifiedAtUtc = DateTime.UtcNow;
                                folder.LastSeenSyncId = syncId;
                                logger.LogInformation($"Created new folder in DAM: {relativePath}");
                            }
                            else
                            {
                                logger.LogError($"Failed to create new folder in DAM: {relativePath}");
                            }
                        }
                        else
                        {
                            logger.LogError($"Parent folder does not exist in DAM: {parentPath}");
                        }
                    }

                    // track as new directory reference
                    await uow.DirectoryRepository.AddAsync(folder, ct);

                }

                var dbSavedResponse = await uow.SaveChangesAsync(ct);

            }
            catch (IOException) { /* skip */ }
            catch (UnauthorizedAccessException) { /* skip */ }

        }

        foreach (var file in Directory.EnumerateFiles(absPath, "*", opts))
        {
            ct.ThrowIfCancellationRequested();

            var destinationPath = syncJob!.DestinationPath.Replace('\\', '/');

            var directoryPath = (Path.GetDirectoryName(file) ?? throw new InvalidOperationException("File path cannot be null")).Replace('\\', '/');
            var damDirectoryPath = directoryPath.Replace(destinationPath, syncJob.VolumePath);

            string filePath = file.Replace('\\', '/');

            string rootPath = syncJob!.DestinationPath.Replace('\\', '/');
            string volumePath = syncJob.VolumePath.Replace('\\', '/');

            string damFilePath = filePath.Replace(rootPath, volumePath);
            string damFileDirectoryPath = damFilePath.Replace(Path.GetFileName(file), string.Empty).TrimEnd('/');

            var relativePath = FileUtils.GetRelative(volumePath, damDirectoryPath);

            var checkFileExistApiResponse = await api.CheckFileExists(tenantId, jobId, damFilePath);

            var checksum = await FileUtils.Sha256Async(file);

            if (checkFileExistApiResponse != null && checkFileExistApiResponse.IsSuccessStatusCode && checkFileExistApiResponse.Content != null)
            {
                string fileName = Path.GetFileName(file);

                var fileSummary = checkFileExistApiResponse.Content;

                if (fileSummary.Success)
                {
                    var dbRef = await uow.FileEntityRepository.Query().Where(f => f.TenantId == tenantId && f.FolderPath == relativePath).FirstOrDefaultAsync(ct);

                    if (dbRef != null)
                    {
                        if(dbRef.ChecksumHash != checksum)
                        {
                            // re upload file
                            var replacementDetails = await api.GetUploadDetailsForFileReplacement(tenantId, fileSummary.ItemId, ct);

                            var s3InfoApiResponse = await api.GetS3Info(tenantId, ct);
                            var s3Info = s3InfoApiResponse.Content!;
                            var parsedToken = new ParsedToken(s3Info.Id, RegionMapper.FromHint(s3Info.Region), s3Info.Key, s3Info.Secret);

                            var s3 = new AmazonS3Client(s3Info.Key, s3Info.Secret, Amazon.RegionEndpoint.EUWest1);

                            // Prepare uploader (from our earlier class)
                            var uploader = new S3FileUploader(s3, bucketName: s3Info.Bucket, baseDirectory: s3Info.Id);
                            uploader.Progress += Console.WriteLine;
                            await uploader.UploadAsync(filePath, replacementDetails.Content!.Key);

                            await api.ReplaceAsset(tenantId, fileSummary.ItemId, ct);
                        }

                        dbRef.Id = fileSummary.ItemId;
                        dbRef.FolderPath = relativePath;
                        // dbRef.DirectoryId = fileSummary.DirectoryId;
                        dbRef.ChecksumHash = checksum;                        
                        dbRef.LastSeenSyncId = syncId;

                        await uow.FileEntityRepository.UpdateAsync(dbRef, ct);
                    }
                    else 
                    {
                        dbRef = new FileEntity
                        {
                            Id = fileSummary.ItemId,
                            ChecksumHash = checksum,
                            LastSeenSyncId = syncId,
                            TenantId = tenantId,
                            FolderPath = relativePath,
                            FileName = fileName,
                            SizeInBytes = new FileInfo(file).Length,
                            CreatedAtUtc = DateTime.UtcNow,
                            LastModifiedAtUtc = DateTime.UtcNow
                        };

                        await uow.FileEntityRepository.AddAsync(dbRef, ct);
                    }
                }
                else 
                {
                    // create dam item
                    // get upload details
                    // upload file into s3 bucket
                    // create list-item in dam
                    // update file db reference
                   
                    var uploadDetailsResponse = await api.GetUploadDetails(tenantId, jobId, damFileDirectoryPath, fileName);
                    var s3InfoApiResponse = await api.GetS3Info(tenantId, ct);
                    var s3Info = s3InfoApiResponse.Content!;
                    var parsedToken = new ParsedToken(s3Info.Id, RegionMapper.FromHint(s3Info.Region), s3Info.Key, s3Info.Secret);

                    var s3 = new AmazonS3Client(s3Info.Key, s3Info.Secret, Amazon.RegionEndpoint.EUWest1);

                    // Prepare uploader (from our earlier class)
                    var uploader = new S3FileUploader(s3, bucketName: s3Info.Bucket, baseDirectory: s3Info.Id);
                    uploader.Progress += Console.WriteLine;
                    await uploader.UploadAsync(filePath, uploadDetailsResponse.Content!.Key);

                    var damItem = new CreateItemV2Request 
                    {
                        Reference = uploadDetailsResponse.Content!.Reference,
                        FileSize = new FileInfo(filePath).Length,
                        LoadMetadataFromFile = true,
                    };

                    var itemCreateApiResponse = await api.CreateItemV2(tenantId, damItem);

                    var createItemResponse = itemCreateApiResponse.Content;

                    if(createItemResponse != null && createItemResponse.Status == "Success")
                    {
                        var dbRef = new FileEntity
                        {
                            Id = createItemResponse.Id,
                            ChecksumHash = checksum,
                            LastSeenSyncId = syncId,
                            TenantId = tenantId,
                            FolderPath = relativePath,
                            FileName = fileName,
                            SizeInBytes = new FileInfo(file).Length,
                            CreatedAtUtc = DateTime.UtcNow,
                            LastModifiedAtUtc = DateTime.UtcNow
                        };

                        await uow.FileEntityRepository.AddAsync(dbRef, ct);
                    }
                }
            }

            logger.LogInformation($"Processing file: {file}");
        }

        await uow.SaveChangesAsync(ct);

        //var missingDirectories = await uow.DirectoryRepository.Query()
        //    .Where(d => d.TenantId == tenantId && d.LastSeenSyncId != syncId && !d.IsDeleted)
        //    .ToListAsync();


        var missingDirectories = await uow.DirectoryRepository.Query().Where(d => d.TenantId == tenantId && d.LastSeenSyncId != syncId).ToListAsync();

        foreach (var missingDirectory in missingDirectories)
        {
            var destinationPath = syncJob!.DestinationPath;

            // Remove leading slash
            // Make sure the relative part uses the OS separator
            string cleanedPath = missingDirectory.Path
                .TrimStart('/', '\\')
                .Replace('/', Path.DirectorySeparatorChar)
                .Replace('\\', Path.DirectorySeparatorChar);

            string fullPath = Path.Combine(destinationPath, cleanedPath);

            if(Directory.Exists(fullPath))
            {
                // directory exists, mark as seen
                missingDirectory.LastSeenSyncId = syncId;
                await uow.DirectoryRepository.UpdateAsync(missingDirectory);
                continue;
            }
            else
            {
                // directory does not exist, mark as inactive

                var deleteFolderApiResponse = await api.DeleteFolder(tenantId, jobId, missingDirectory.Id);

                if(deleteFolderApiResponse != null && deleteFolderApiResponse.IsSuccessful)
                {
                    missingDirectory.IsDeleted = false;
                    await uow.DirectoryRepository.UpdateAsync(missingDirectory);
                }
            }
        }

        await uow.FileEntityRepository.Query()
            .Where(d => d.TenantId == tenantId && d.LastSeenSyncId != syncId && !d.IsDeleted)
            .ForEachAsync(async file => {

                var destinationPath = syncJob!.DestinationPath;

                // Remove leading slash
                // Make sure the relative part uses the OS separator
                string cleanedPath = file.FolderPath
                    .TrimStart('/', '\\')
                    .Replace('/', Path.DirectorySeparatorChar)
                    .Replace('\\', Path.DirectorySeparatorChar);

                string fullPath = Path.Combine(destinationPath, cleanedPath, file.FileName);

                if (File.Exists(fullPath))
                {
                    // file exists, mark as seen
                    file.LastSeenSyncId = syncId;
                    return;
                }
                else 
                {


                    var delteItemApiResponse = await api.DeleteItem(tenantId, jobId, file.Id);

                    if (delteItemApiResponse != null && delteItemApiResponse.IsSuccessful)
                    {
                        file.IsDeleted = true;
                        await uow.FileEntityRepository.UpdateAsync(file, ct);
                    }
                    else
                    {
                        logger.LogError($"Failed to delete item in DAM: {file.Id}");
                    }
                }

                logger.LogInformation($"Marking file as inactive: {file.FileName} in {file.FolderPath}");
            });

        await uow.SaveChangesAsync(ct);

        await Task.Delay(1 * 60 * 1000, ct);
        logger.LogInformation($"Job: {jobId} finished");
    }
}
