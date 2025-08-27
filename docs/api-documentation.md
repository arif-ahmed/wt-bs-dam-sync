# DAM API Documentation

This document provides comprehensive documentation for the Brandshare DAM API client interface, including all available endpoints, request/response formats, and usage examples.

## Overview

The DAM API client is implemented using **Refit** for type-safe HTTP communication with the Brandshare Digital Asset Management system. All API calls include built-in resilience patterns (retry, circuit breaker, timeout) via **Polly**.

## Authentication

All API endpoints require authentication via API key passed in the request header:

```http
Require-ApiKey: true
TenantId: {tenant-id}
Authorization: Bearer {api-key}
```

## API Endpoints

### Folder Operations

#### Get Folders

Retrieves the folder structure for a specific job.

```csharp
[Get(\"/FileSync/GetFolders\")]
Task<ApiResponse<List<FolderSummary>>> GetFolders(
    [Header(ApiHeaders.TenantId)] string tenantId, 
    [AliasAs(\"jobId\")] string jobId);
```

**Parameters:**
- `tenantId` (Header): Tenant identifier
- `jobId` (Query): Sync job identifier

**Response:**
```json
{
  \"folders\": [
    {
      \"id\": \"folder-123\",
      \"parentId\": \"parent-456\",
      \"path\": \"/documents/projects\",
      \"label\": \"Projects\",
      \"isActive\": true,
      \"createdAt\": \"2024-01-15T10:00:00Z\",
      \"modifiedAt\": \"2024-01-20T15:30:00Z\"
    }
  ]
}
```

#### Create Folder

Creates a new folder in the DAM system.

```csharp
[Get(\"/FileSync/CreateFolder\")]
Task<ApiResponse<CreateFolderResponse>> CreateFolder(
    [Header(ApiHeaders.TenantId)] string tenantId,
    [AliasAs(\"jobId\")] string jobId,
    [AliasAs(\"parentFolderId\")] string parentFolderId,
    [AliasAs(\"folderName\")] string folderName);
```

**Parameters:**
- `tenantId` (Header): Tenant identifier
- `jobId` (Query): Sync job identifier
- `parentFolderId` (Query): Parent folder ID
- `folderName` (Query): Name for the new folder

**Response:**
```json
{
  \"success\": true,
  \"folderId\": \"new-folder-789\",
  \"message\": \"Folder created successfully\"
}
```

#### Check Folder Exists

Checks if a folder exists at the specified path.

```csharp
[Get(\"/FileSync/CheckFolderExists\")]
Task<ApiResponse<BoolSuccessResponse>> CheckFolderExists(
    [Header(ApiHeaders.TenantId)] string tenantId,
    [AliasAs(\"jobId\")] string jobId,
    [AliasAs(\"folderPath\")] string encodedPath);
```

**Parameters:**
- `tenantId` (Header): Tenant identifier
- `jobId` (Query): Sync job identifier
- `encodedPath` (Query): URL-encoded folder path

**Response:**
```json
{
  \"success\": true,
  \"exists\": true
}
```

#### Delete Folder

Deletes a folder from the DAM system.

```csharp
[Get(\"/FileSync/DeleteFolder\")]
Task<ApiResponse<StatusResponse>> DeleteFolder(
    [Header(ApiHeaders.TenantId)] string tenantId,
    [AliasAs(\"jobId\")] string jobId,
    [AliasAs(\"folderId\")] string folderId);
```

#### Rename Folder

Renames an existing folder.

```csharp
[Get(\"/FileSync/RenameFolder\")]
Task<ApiResponse<StatusResponse>> RenameFolder(
    [AliasAs(\"jobId\")] string jobId,
    [AliasAs(\"folderId\")] string folderId,
    [AliasAs(\"newFolderName\")] string newFolderName);
```

#### Move Folder

Moves a folder to a new parent location.

```csharp
[Get(\"/FileSync/MoveFolder\")]
Task<ApiResponse<StatusResponse>> MoveFolder(
    [AliasAs(\"jobId\")] string jobId,
    [AliasAs(\"folderId\")] string folderId,
    [AliasAs(\"targetFolderId\")] string targetFolderId);
```

### File Operations

#### Check File Exists

Checks if a file exists at the specified path.

```csharp
[Get(\"/FileSync/CheckFileExists\")]
Task<ApiResponse<FileExistsResponse>> CheckFileExists(
    [Header(ApiHeaders.TenantId)] string tenantId,
    [AliasAs(\"jobId\")] string jobId,
    [AliasAs(\"filePath\")] string encodedPath);
```

**Response:**
```json
{
  \"exists\": true,
  \"fileId\": \"file-456\",
  \"lastModified\": \"2024-01-20T15:30:00Z\",
  \"sizeBytes\": 1024000
}
```

#### Get Items by Folder

Retrieves all items (files and subfolders) within a specific folder.

```csharp
[Get(\"/FileSync/GetItemsByFolderId\")]
Task<ApiResponse<DamResponse>> GetItemsByFolderId(
    [Query] GetItemsByFolderIdQuery query,
    CancellationToken cancellationToken = default);
```

**Query Parameters:**
```csharp
public class GetItemsByFolderIdQuery
{
    public string JobId { get; set; }
    public string FolderId { get; set; }
    public int? PageSize { get; set; } = 50;
    public int? PageNumber { get; set; } = 1;
    public bool IncludeSubfolders { get; set; } = false;
}
```

**Response:**
```json
{
  \"items\": [
    {
      \"itemId\": \"item-123\",
      \"fileName\": \"document.pdf\",
      \"filePath\": \"/downloads/document.pdf\",
      \"fileId\": \"file-456\",
      \"folderPath\": \"/documents\",
      \"modifiedAt\": \"2024-01-20T15:30:00Z\",
      \"modifiedAtEpochMs\": 1705849800000,
      \"sizeBytes\": 1024000,
      \"mimeType\": \"application/pdf\"
    }
  ],
  \"pagination\": {
    \"pageNumber\": 1,
    \"pageSize\": 50,
    \"totalItems\": 150,
    \"totalPages\": 3
  }
}
```

#### Create Item V2

Creates a new file item in the DAM system.

```csharp
[Post(\"/FileSync/CreateItemV2\")]
Task<ApiResponse<CreateItemResponse>> CreateItemV2(
    [Header(ApiHeaders.TenantId)] string tenantId, 
    [Body] CreateItemV2Request request);
```

**Request Body:**
```json
{
  \"jobId\": \"job-123\",
  \"fileName\": \"document.pdf\",
  \"folderPath\": \"/documents\",
  \"fileSize\": 1024000,
  \"mimeType\": \"application/pdf\",
  \"checksum\": \"sha256-hash\",
  \"s3Key\": \"uploads/file-key\",
  \"metadata\": {
    \"author\": \"John Doe\",
    \"created\": \"2024-01-20T15:30:00Z\"
  }
}
```

**Response:**
```json
{
  \"success\": true,
  \"itemId\": \"new-item-789\",
  \"message\": \"Item created successfully\"
}
```

#### Delete Item

Deletes a file item from the DAM system.

```csharp
[Get(\"/FileSync/DeleteItem\")]
Task<ApiResponse<StatusResponse>> DeleteItem(
    [Header(ApiHeaders.TenantId)] string tenantId,
    [AliasAs(\"jobId\")] string jobId,
    [AliasAs(\"itemId\")] string itemId);
```

#### Get Modified Items

Retrieves items modified after a specific timestamp.

```csharp
[Get(\"/FileSync/GetModifiedItems\")]
Task<ApiResponse<ModifiedItemsResponse>> GetModifiedItems(
    [Header(ApiHeaders.TenantId)] string tenantId,
    [AliasAs(\"jobId\")] string jobId,
    [AliasAs(\"volumeName\")] string volumeName,
    [AliasAs(\"isActive\")] bool isActive,
    [AliasAs(\"lastItemId\")] string? lastItemId = null,
    [AliasAs(\"pageSize\")] int? pageSize = null,
    [AliasAs(\"modifiedAfterAt\")] long? modifiedAfterAt = null,
    CancellationToken cancellationToken = default);
```

**Parameters:**
- `lastItemId`: For pagination, ID of the last item from previous page
- `pageSize`: Number of items per page (default: 50)
- `modifiedAfterAt`: Unix timestamp in milliseconds

**Response:**
```json
{
  \"items\": [
    {
      \"itemId\": \"item-123\",
      \"fileName\": \"document.pdf\",
      \"filePath\": \"/downloads/document.pdf\",
      \"folderPath\": \"/documents\",
      \"modifiedAt\": \"2024-01-20T15:30:00Z\",
      \"modifiedAtEpochMs\": 1705849800000,
      \"changeType\": \"Modified\"
    }
  ],
  \"hasMore\": true,
  \"lastItemId\": \"item-456\"
}
```

### Upload Operations

#### Get Upload Details

Retrieves S3 upload URL and metadata for file upload.

```csharp
[Get(\"/FileSync/GetUploadDetails\")]
Task<ApiResponse<UploadDetailsResponse>> GetUploadDetails(
    [Header(ApiHeaders.TenantId)] string tenantId,
    [AliasAs(\"jobId\")] string jobId,
    [AliasAs(\"directoryPath\")] string directoryPath,
    [AliasAs(\"fileName\")] string fileName);
```

**Response:**
```json
{
  \"uploadUrl\": \"https://s3.amazonaws.com/bucket/uploads/file-key\",
  \"s3Key\": \"uploads/file-key\",
  \"fields\": {
    \"key\": \"uploads/file-key\",
    \"AWSAccessKeyId\": \"AKIA...\",
    \"policy\": \"eyJ...\",
    \"signature\": \"abc123\"
  },
  \"maxSizeBytes\": 104857600,
  \"expiresAt\": \"2024-01-20T16:30:00Z\"
}
```

#### Get Upload Details for File Replacement

Retrieves upload details for replacing an existing file.

```csharp
[Get(\"/Dam/GetUploadDetailsForFileReplacement\")]
Task<ApiResponse<UploadDetailsResponse>> GetUploadDetailsForFileReplacement(
    [Header(ApiHeaders.TenantId)] string tenantId,
    [AliasAs(\"listItemId\")] string listItemId,
    CancellationToken cancellationToken = default);
```

### Asset Operations

#### Replace Asset

Replaces an existing asset with a new version.

```csharp
[Get(\"/Dam/ReplaceAsset\")]
Task<ApiResponse<StatusResponse>> ReplaceAsset(
    [Header(ApiHeaders.TenantId)] string tenantId, 
    [AliasAs(\"listItemId\")] string listItemId, 
    CancellationToken cancellationToken = default);
```

#### Move Asset

Moves an asset to a different folder.

```csharp
[Get(\"/Dam/MoveAsset\")]
Task<ApiResponse<StatusResponse>> MoveAsset(
    [AliasAs(\"itemId\")] string itemId,
    [AliasAs(\"targetFolderId\")] string targetFolderId);
```

#### Rename File

Renames an existing file.

```csharp
[Get(\"/Dam/RenameFile\")]
Task<ApiResponse<StatusResponse>> RenameFile(
    [AliasAs(\"itemId\")] string itemId,
    [AliasAs(\"newFileName\")] string newFileName);
```

### Job Management

#### Get Job List

Retrieves the list of sync jobs for a specific machine.

```csharp
[Get(\"/FileSyncJob/GetJobList\")]
Task<ApiResponse<JobListResponse>> GetJobList(
    [Header(ApiHeaders.TenantId)] string tenantId, 
    [AliasAs(\"machineName\")] string machineName);
```

**Response:**
```json
{
  \"jobs\": [
    {
      \"id\": \"job-123\",
      \"jobName\": \"Documents Sync\",
      \"volumeName\": \"Documents\",
      \"volumePath\": \"/documents\",
      \"destinationPath\": \"C:\\\\Sync\\\\Documents\",
      \"syncDirection\": \"Both\",
      \"jobIntervalMinutes\": 60,
      \"isActive\": true,
      \"jobStatus\": \"Running\",
      \"lastRunTime\": 1705849800000,
      \"nextRunTime\": 1705853400000
    }
  ]
}
```

#### Update Job

Updates job status and telemetry information.

```csharp
[Get(\"/FileSyncJob/UpdateJob\")]
Task<ApiResponse<StatusResponse>> UpdateJob(
    [AliasAs(\"jobId\")] string jobId,
    [AliasAs(\"destinationPath\")] string? destinationPath = null,
    [AliasAs(\"lastRunTime\")] long? lastRunTime = null,
    [AliasAs(\"nextRunTime\")] long? nextRunTime = null,
    [AliasAs(\"jobStatus\")] string? jobStatus = null,
    [AliasAs(\"affectedFolders\")] int? affectedFolders = null,
    [AliasAs(\"affectedFiles\")] int? affectedFiles = null);
```

### System Operations

#### Test Connection

Tests connectivity to the DAM system.

```csharp
[Get(\"/FileSyncMachine/TestConnection\")]
Task<ApiResponse<bool?>> TestConnection();
```

**Response:**
```json
{
  \"connected\": true,
  \"responseTime\": 150,
  \"version\": \"2.1.0\"
}
```

#### Get Sync Machine Time Interval

Retrieves the recommended sync interval for a machine.

```csharp
[Get(\"/FileSyncMachine/GetSyncMachineTimeInterval\")]
Task<ApiResponse<int>> GetSyncMachineTimeInterval(
    [AliasAs(\"machineName\")] string machineName);
```

#### Get S3 Info

Retrieves S3 configuration information.

```csharp
[Get(\"/FileSync/GetS3Info\")]
Task<ApiResponse<S3InfoResponse>> GetS3Info(
    [Header(ApiHeaders.TenantId)] string tenantId,
    CancellationToken cancellationToken = default);
```

**Response:**
```json
{
  \"bucketName\": \"dam-assets-bucket\",
  \"region\": \"us-east-1\",
  \"accessKeyId\": \"AKIA...\",
  \"sessionToken\": \"temp-token\",
  \"expiresAt\": \"2024-01-20T16:30:00Z\"
}
```

### Logging

#### Save Log

Sends log entries to the DAM system.

```csharp
[Post(\"/FileSyncLog/save\")]
Task<ApiResponse<StatusResponse>> SaveLog([Body] SaveLogRequest request);
```

**Request Body:**
```json
{
  \"jobId\": \"job-123\",
  \"level\": \"Information\",
  \"message\": \"Sync completed successfully\",
  \"timestamp\": \"2024-01-20T15:30:00Z\",
  \"exception\": null,
  \"properties\": {
    \"filesProcessed\": 42,
    \"duration\": \"00:02:15\"
  }
}
```

## Response Models

### Common Response Types

```csharp
public class StatusResponse
{
    public bool Success { get; set; }
    public string Message { get; set; }
    public string? ErrorCode { get; set; }
}

public class BoolSuccessResponse
{
    public bool Success { get; set; }
    public bool Result { get; set; }
}

public class ApiResponse<T>
{
    public bool IsSuccessStatusCode { get; set; }
    public HttpStatusCode StatusCode { get; set; }
    public string? ReasonPhrase { get; set; }
    public T? Content { get; set; }
    public string? Error { get; set; }
}
```

### Folder Models

```csharp
public class FolderSummary
{
    public string Id { get; set; }
    public string? ParentId { get; set; }
    public string Path { get; set; }
    public string Label { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime ModifiedAt { get; set; }
}
```

### File Models

```csharp
public class DamItem
{
    public string ItemId { get; set; }
    public string FileName { get; set; }
    public string FilePath { get; set; }
    public string? FileId { get; set; }
    public string FolderPath { get; set; }
    public DateTime ModifiedAt { get; set; }
    public long ModifiedAtEpochMs { get; set; }
    public long SizeBytes { get; set; }
    public string? MimeType { get; set; }
    public string? ChecksumHash { get; set; }
}
```

## Error Handling

### Standard HTTP Status Codes

- **200 OK**: Request successful
- **400 Bad Request**: Invalid request parameters
- **401 Unauthorized**: Invalid or missing API key
- **403 Forbidden**: Insufficient permissions
- **404 Not Found**: Resource not found
- **429 Too Many Requests**: Rate limit exceeded
- **500 Internal Server Error**: Server error

### Error Response Format

```json
{
  \"success\": false,
  \"message\": \"Validation failed\",
  \"errorCode\": \"INVALID_PARAMETERS\",
  \"details\": {
    \"fileName\": \"File name is required\",
    \"folderPath\": \"Invalid folder path format\"
  }
}
```

### Resilience Policies

The API client implements automatic resilience patterns:

1. **Retry Policy**: Exponential backoff for transient failures
2. **Circuit Breaker**: Prevents cascade failures
3. **Timeout Policy**: Prevents hung requests

```csharp
// Retry configuration
.WaitAndRetryAsync(
    retryCount: 3,
    sleepDurationProvider: attempt => 
        TimeSpan.FromSeconds(Math.Pow(2, attempt)) + 
        TimeSpan.FromMilliseconds(Random.Shared.Next(0, 250)))

// Circuit breaker configuration
.CircuitBreakerAsync(5, TimeSpan.FromSeconds(30))

// Timeout configuration
.TimeoutAsync(TimeSpan.FromSeconds(45))
```

## Usage Examples

### Basic API Call

```csharp
public class SyncService
{
    private readonly IBrandShareDamApi _api;
    private readonly ITenantContext _tenantContext;
    
    public async Task<List<FolderSummary>> GetJobFolders(string jobId)
    {
        var tenantId = await _tenantContext.GetCurrentTenantId();
        var response = await _api.GetFolders(tenantId, jobId);
        
        if (response.IsSuccessStatusCode)
        {
            return response.Content ?? new List<FolderSummary>();
        }
        
        throw new ApiException($\"Failed to get folders: {response.ReasonPhrase}\");
    }
}
```

### Streaming Modified Items

```csharp
public async IAsyncEnumerable<DamItem> StreamModifiedItems(
    string jobId, 
    string volumeName,
    long? modifiedAfterAt = null,
    [EnumeratorCancellation] CancellationToken cancellationToken = default)
{
    var tenantId = await _tenantContext.GetCurrentTenantId();
    var lastItemId = string.Empty;
    var pageSize = 50;
    
    while (true)
    {
        var response = await _api.GetModifiedItems(
            tenantId, jobId, volumeName, true, 
            lastItemId, pageSize, modifiedAfterAt, 
            cancellationToken);
            
        if (!response.IsSuccessStatusCode || 
            response.Content?.Items == null || 
            !response.Content.Items.Any())
        {
            break;
        }
        
        foreach (var item in response.Content.Items)
        {
            yield return item;
        }
        
        if (!response.Content.HasMore)
        {
            break;
        }
        
        lastItemId = response.Content.LastItemId;
    }
}
```

### File Upload Workflow

```csharp
public async Task UploadFile(string jobId, string filePath, string fileName)
{
    var tenantId = await _tenantContext.GetCurrentTenantId();
    
    // 1. Get upload details
    var uploadResponse = await _api.GetUploadDetails(
        tenantId, jobId, Path.GetDirectoryName(filePath), fileName);
        
    if (!uploadResponse.IsSuccessStatusCode)
    {
        throw new ApiException(\"Failed to get upload details\");
    }
    
    var uploadDetails = uploadResponse.Content;
    
    // 2. Upload to S3
    await UploadToS3(filePath, uploadDetails);
    
    // 3. Create DAM item
    var createRequest = new CreateItemV2Request
    {
        JobId = jobId,
        FileName = fileName,
        FolderPath = Path.GetDirectoryName(filePath),
        S3Key = uploadDetails.S3Key,
        FileSize = new FileInfo(filePath).Length
    };
    
    var createResponse = await _api.CreateItemV2(tenantId, createRequest);
    
    if (!createResponse.IsSuccessStatusCode)
    {
        throw new ApiException(\"Failed to create DAM item\");
    }
}
```

This documentation provides comprehensive coverage of the DAM API client interface, enabling developers to effectively integrate with the Brandshare DAM system.