using BrandshareDamSync.Daemon.Infrastructure.Http;
using Refit;

namespace BrandshareDamSync.Infrastructure.BrandShareDamClient;

public interface IBrandShareDamApi
{
    // ===== FOLDER APIs =====
    // ?? GetFolders
    [Headers("Require-ApiKey: true")]
    [Get("/FileSync/GetFolders")]
    Task<ApiResponse<List<FolderSummary>>> GetFolders([Header(ApiHeaders.TenantId)] string tenantId, [AliasAs("jobId")] string jobId);

    // ? CreateFolder
    [Headers("Require-ApiKey: true")]
    [Get("/FileSync/CreateFolder")]
    Task<ApiResponse<CreateFolderResponse>> CreateFolder(
        [Header(ApiHeaders.TenantId)] string tenantId,
        [AliasAs("jobId")] string jobId,
        [AliasAs("parentFolderId")] string parentFolderId,
        [AliasAs("folderName")] string folderName);

    // ?? CheckFolderExists
    [Headers("Require-ApiKey: true")]
    [Get("/FileSync/CheckFolderExists")]
    Task<ApiResponse<BoolSuccessResponse>> CheckFolderExists(
        [Header(ApiHeaders.TenantId)] string tenantId,
        [AliasAs("jobId")] string jobId,
        [AliasAs("folderPath")] string encodedPath);

    // ? DeleteFolder
    [Headers("Require-ApiKey: true")]
    [Get("/FileSync/DeleteFolder")]
    Task<ApiResponse<StatusResponse>> DeleteFolder(
        [Header(ApiHeaders.TenantId)] string tenantId,
        [AliasAs("jobId")] string jobId,
        [AliasAs("folderId")] string folderId);


    // ===== FILE APIs =====

    // ?? CheckFileExists
    [Headers("Require-ApiKey: true")]
    [Get("/FileSync/CheckFileExists")]
    Task<ApiResponse<FileExistsResponse>> CheckFileExists(
        [Header(ApiHeaders.TenantId)] string tenantId,
        [AliasAs("jobId")] string jobId,
        [AliasAs("filePath")] string encodedPath);

    // ?? GetItemsByFolderId (paging optional)
    [Headers("Require-ApiKey: true")]
    [Get("/FileSync/GetItemsByFolderId")]
    Task<ApiResponse<DamResponse>> GetItemsByFolderId(
        [Query] GetItemsByFolderIdQuery query,
        CancellationToken cancellationToken = default
    );

    // ?? CreateItemV2 (POST body)
    [Headers("Require-ApiKey: true")]
    [Post("/FileSync/CreateItemV2")]
    Task<ApiResponse<CreateItemResponse>> CreateItemV2([Header(ApiHeaders.TenantId)] string tenantId, [Body] CreateItemV2Request request);

    // ??? DeleteItem
    [Headers("Require-ApiKey: true")]
    [Get("/FileSync/DeleteItem")]
    Task<ApiResponse<StatusResponse>> DeleteItem(
        [Header(ApiHeaders.TenantId)] string tenantId,
        [AliasAs("jobId")] string jobId,
        [AliasAs("itemId")] string itemId);


    // ===== UPLOAD Integration =====

    // ?? GetUploadDetails
    [Headers("Require-ApiKey: true")]
    [Get("/FileSync/GetUploadDetails")]
    Task<ApiResponse<UploadDetailsResponse>> GetUploadDetails(
        [Header(ApiHeaders.TenantId)] string tenantId,
        [AliasAs("jobId")] string jobId,
        [AliasAs("directoryPath")] string directoryPath,
        [AliasAs("fileName")] string fileName);

    // ?? GetUploadDetailsForFileReplacement
    [Headers("Require-ApiKey: true")]
    [Get("/Dam/GetUploadDetailsForFileReplacement")]
    Task<ApiResponse<UploadDetailsResponse>> GetUploadDetailsForFileReplacement(
        [Header(ApiHeaders.TenantId)] string tenantId,
        [AliasAs("listItemId")] string listItemId,
        CancellationToken cancellationToken = default);


    // ===== ASSET Actions =====

    // ?? ReplaceAsset
    [Headers("Require-ApiKey: true")]
    [Get("/Dam/ReplaceAsset")]
    Task<ApiResponse<StatusResponse>> ReplaceAsset([Header(ApiHeaders.TenantId)] string tenantId, [AliasAs("listItemId")] string listItemId, CancellationToken cancellationToken = default);

    // ??? MoveAsset
    [Headers("Require-ApiKey: true")]
    [Get("/Dam/MoveAsset")]
    Task<ApiResponse<StatusResponse>> MoveAsset(
        [AliasAs("itemId")] string itemId,
        [AliasAs("targetFolderId")] string targetFolderId);

    // ?? RenameFile
    [Headers("Require-ApiKey: true")]
    [Get("/Dam/RenameFile")]
    Task<ApiResponse<StatusResponse>> RenameFile(
        [AliasAs("itemId")] string itemId,
        [AliasAs("newFileName")] string newFileName);


    // ===== SYNC JOB APIs =====

    // ?? GetJobList
    [Headers("Require-ApiKey: true")]
    [Get("/FileSyncJob/GetJobList")]
    //Task<ApiResponse<JobListResponse>> GetJobList([AliasAs("machineName")] string machineName);
    Task<ApiResponse<JobListResponse>> GetJobList([Header(ApiHeaders.TenantId)] string tenantId, [AliasAs("machineName")] string machineName);

    // ?? UpdateJob (telemetry/logging) – using GET as per your spec
    [Headers("Require-ApiKey: true")]
    [Get("/FileSyncJob/UpdateJob")]
    Task<ApiResponse<StatusResponse>> UpdateJob(
        [AliasAs("jobId")] string jobId,
        [AliasAs("destinationPath")] string? destinationPath = null,
        [AliasAs("lastRunTime")] long? lastRunTime = null,
        [AliasAs("nextRunTime")] long? nextRunTime = null,
        [AliasAs("jobStatus")] string? jobStatus = null,
        [AliasAs("affectedFolders")] int? affectedFolders = null,
        [AliasAs("affectedFiles")] int? affectedFiles = null);


    // ===== SYSTEM APIs =====

    // ?? TestConnection
    [Headers("Require-ApiKey: true")]
    [Get("/FileSyncMachine/TestConnection")]
    Task<ApiResponse<bool?>> TestConnection();

    // ?? GetSyncMachineTimeInterval
    [Headers("Require-ApiKey: true")]
    [Get("/FileSyncMachine/GetSyncMachineTimeInterval")]
    Task<ApiResponse<int>> GetSyncMachineTimeInterval([AliasAs("machineName")] string machineName);

    // ?? GetFoldersAndFilesCount
    [Headers("Require-ApiKey: true")]
    [Get("/FileSync/GetFoldersAndFilesCount")]
    Task<ApiResponse<FoldersFilesCountResponse>> GetFoldersAndFilesCount([AliasAs("jobId")] string jobId);

    // ?? Save log (POST)
    [Headers("Require-ApiKey: true")]
    [Post("/FileSyncLog/save")]
    Task<ApiResponse<StatusResponse>> SaveLog([Body] SaveLogRequest request);

    [Headers("Require-ApiKey: true")]
    [Get("/FileSync/GetModifiedItems")]
    Task<ApiResponse<ModifiedItemsResponse>> GetModifiedItems(
        [Header(ApiHeaders.TenantId)] string tenantId,
        [AliasAs("jobId")] string jobId,
        [AliasAs("volumeName")] string volumeName,
        [AliasAs("isActive")] bool isActive,
        [AliasAs("lastItemId")] string? lastItemId = null,     // ? nullable + default null
        [AliasAs("pageSize")] int? pageSize = null,
        [AliasAs("modifiedAfterAt")] long? modifiedAfterAt = null,
        CancellationToken cancellationToken = default);


    // Optional raw version if you need plain JSON
    [Headers("Require-ApiKey: true")]
    [Get("/FileSync/GetModifiedItems")]
    Task<ApiResponse<string>> GetModifiedItemsRaw(
        [AliasAs("jobId")] string jobId,
        [AliasAs("volumeName")] string volumeName,
        [AliasAs("isActive")] bool isActive,
        [AliasAs("lastItemId")] string? lastItemId = null,
        [AliasAs("pageSize")] int? pageSize = null,
        [AliasAs("modifiedAfterAt")] long? modifiedAfterAt = null
    );

    // --- ITEM_GET: /FileSync/GetItem?jobId=...&itemId=...
    [Headers("Require-ApiKey: true")]
    [Get("/FileSync/GetItem")]
    Task<ApiResponse<DamItemDetails>> GetItem(
        [AliasAs("jobId")] string jobId,
        [AliasAs("itemId")] string itemId
    );

    // Raw fallback if the schema is still in flux
    [Headers("Require-ApiKey: true")]
    [Get("/FileSync/GetItem")]
    Task<ApiResponse<string>> GetItemRaw(
        [AliasAs("jobId")] string jobId,
        [AliasAs("itemId")] string itemId
    );

    // --- S3_INFO_GET: /FileSync/GetS3Info  (shape unknown; return raw for now)
    [Headers("Require-ApiKey: true")]
    [Get("/FileSync/GetS3Info")]
    Task<ApiResponse<S3InfoResponse>> GetS3Info(
        [Header(ApiHeaders.TenantId)] string tenantId,
        CancellationToken cancellationToken = default);

    // If you know the exact shape, define a DTO and switch to it later.
    // e.g. Task<ApiResponse<S3InfoResponse>> GetS3Info();

    // --- RENAME_FOLDER: /FileSync/RenameFolder?jobId=...&folderId=...&newFolderName=...
    [Headers("Require-ApiKey: true")]
    [Get("/FileSync/RenameFolder")]
    Task<ApiResponse<StatusResponse>> RenameFolder(
        [AliasAs("jobId")] string jobId,
        [AliasAs("folderId")] string folderId,
        [AliasAs("newFolderName")] string newFolderName
    );

    // --- MOVE_FOLDER: /FileSync/MoveFolder?jobId=...&folderId=...&targetFolderId=...
    // (Assumed param names based on your MoveAsset pattern.)
    [Headers("Require-ApiKey: true")]
    [Get("/FileSync/MoveFolder")]
    Task<ApiResponse<StatusResponse>> MoveFolder(
        [AliasAs("jobId")] string jobId,
        [AliasAs("folderId")] string folderId,
        [AliasAs("targetFolderId")] string targetFolderId
    );

}
