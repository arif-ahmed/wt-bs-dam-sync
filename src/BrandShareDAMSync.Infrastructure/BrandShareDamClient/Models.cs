using BrandshareDamSync.Domain;
using System.Text.Json.Serialization;

namespace BrandshareDamSync.Infrastructure.BrandShareDamClient;

public sealed class FolderItem
{
    public string Id { get; set; } = "";
    public bool IsActive { get; set; }
    public string FieldId { get; set; } = "";
    public string ParentId { get; set; } = "";
    public string Label { get; set; } = "";
    public string Path { get; set; } = "";
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset ModifiedAt { get; set; }

    // Optional helpers:
    public string NormalisedPath => Path?.Replace("\\", "/") ?? "";
    public string[] Segments => NormalisedPath.Trim('/').Split('/', StringSplitOptions.RemoveEmptyEntries);
}

// ---------- Common ----------
public sealed class StatusResponse
{
    public string? Status { get; set; }
    public string? ErrorMessage { get; set; }
    public string? ErrorDetails { get; set; }
    public object? Data { get; set; }
}

public sealed class BoolSuccessResponse
{
    public bool Success { get; set; }
    public string FolderId { get; set; } = "";
    public string? ErrorMessage { get; set; }
}

// ---------- Folders ----------
public sealed class FolderSummary
{
    public string Id { get; set; } = "";
    public bool IsActive { get; set; }
    public string FieldId { get; set; } = "";
    public string ParentId { get; set; } = "";
    public string Label { get; set; } = "";
    public string Path { get; set; } = "";
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset ModifiedAt { get; set; }
}

public sealed class CreateFolderResponse
{
    public string Status { get; set; } = "";
    public string Id { get; set; } = "";
    public string ErrorMessage { get; set; } = "";
}

// ---------- Files & Items ----------
public sealed class FileExistsResponse
{
    public string ItemId { get; set; } = "";
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
}

public sealed class ItemsByFolderResponse
{
    public bool Success { get; set; }
    public List<DamItem> Items { get; set; } = new();
}

public sealed class DamResponse
{
    public string Status { get; set; } = "";
    public List<DamItem> Items { get; set; } = new();
    public List<string> ErrorItemIds { get; set; } = new();

    // Top-level epoch time is in SECONDS in your sample
    public long LastRunTime { get; set; }
    public string TenantId { get; set; } = "";
}

public sealed class DamItem
{
    public string ItemId { get; set; } = "";
    public string FileName { get; set; } = "";
    public string FolderPath { get; set; } = "";
    public string FilePath { get; set; } = "";
    public string FileId { get; set; } = "";

    public DateTime ModifiedAt { get; set; }       // ISO 8601 string ? DateTime
    public long ModifiedAtEpochMs { get; set; }    // epoch milliseconds
}


// CreateItemV2
public sealed class CreateItemV2Request
{
    //public string JobId { get; set; } = "";
    //public string FolderId { get; set; } = "";
    //public string FileName { get; set; } = "";
    //public long FileSize { get; set; }
    //public Dictionary<string, object>? Metadata { get; set; }

    public string Reference { get; set; } = default!;
    public long FileSize { get; set; }
    public bool LoadMetadataFromFile { get; set; }
}

public sealed class CreateItemResponse
{
    public string Status { get; set; } = "";
    public string Id { get; set; } = "";
}

// ---------- Upload Details ----------
public sealed class UploadDetailsResponse
{
    public string Status { get; set; } = "";
    public string Key { get; set; } = "";
    public UploadDetails Details { get; set; } = new UploadDetails();
    public string Reference { get; set; } = "";
}

public sealed class UploadDetails
{
    public string FileFormField { get; set; } = "";
    public List<AuxFormField> AuxFormField { get; set; } = new();
    public string Url { get; set; } = "";
}

public sealed class AuxFormField
{
    public string Key { get; set; } = "";
    public string Value { get; set; } = "";
}

// public record S3InfoResponse(string S3Info);
public sealed class S3InfoResponse
{
    public string S3Info { get; set; } = "";

    public string[] Parts => S3Info.Split('_');

    public string Id => Parts.Length > 0 ? Parts[0] : "";
    public string Bucket => Parts.Length > 1 ? Parts[1] : "";
    public string Region => Parts.Length > 2 ? Parts[2] : "";
    public string Key => Parts.Length > 3 ? Parts[3] : "";
    public string Secret => Parts.Length > 4 ? Parts[4] : "";
}


// ---------- Sync Jobs ----------
// Root envelope
public sealed class JobListResponse
{
    public bool Success { get; set; }
    public List<ApiSyncJob> Jobs { get; set; } = new();
}

// Job item
public sealed class ApiSyncJob
{
    public string Id { get; set; } = "";
    public string JobName { get; set; } = "";
    public string MachineName { get; set; } = "";
    public string VolumeName { get; set; } = "";
    public string VolumePath { get; set; } = "";
    public string VolumeId { get; set; } = "";
    public string ListId { get; set; } = "";
    public string FolderFieldId { get; set; } = "";

    // JSON: Unix seconds (e.g. 1629612000)
    public long JobStartTime { get; set; }

    // JSON key is "JobInterVal" (capital V). Map it to a nicer C# name.
    [JsonPropertyName("JobInterVal")]
    public int JobInterval { get; set; }

    // "D2L" or "L2D"
    public string SyncDirection { get; set; } = "";

    public string DestinationPath { get; set; } = "";

    public string JobStatus { get; set; } = "";

    // JSON: .NET ticks (100-ns) since 0001-01-01
    public long LastRunTime { get; set; }
    public long NextRunTime { get; set; }

    public bool IsActive { get; set; }
    public string PrimaryLocation { get; set; } = ""; // "server" / "local"
    public string LiveLogApi { get; set; } = "";

    // ---- Convenience properties (not serialised) ----

    [JsonIgnore]
    public DateTimeOffset JobStartAsUtc => DateTimeOffset.FromUnixTimeSeconds(JobStartTime);

    [JsonIgnore]
    public DateTime LastRunAsUtc => new DateTime(LastRunTime, DateTimeKind.Utc);

    [JsonIgnore]
    public DateTime NextRunAsUtc => new DateTime(NextRunTime, DateTimeKind.Utc);

    [JsonIgnore]
    public string DestinationPathTrimmed => DestinationPath?.Trim() ?? "";
}

// UpdateJob (telemetry)
public sealed class UpdateJobRequest
{
    public string JobId { get; set; } = "";
    public string? DestinationPath { get; set; }
    public long? LastRunTime { get; set; }     // ticks/epoch: match server
    public long? NextRunTime { get; set; }
    public string? JobStatus { get; set; }
    public int? AffectedFolders { get; set; }
    public int? AffectedFiles { get; set; }
}

// ---------- System ----------
public sealed class FoldersFilesCountResponse
{
    public int FolderCount { get; set; }
    public int FileCount { get; set; }
}

// Logs
public sealed class SaveLogRequest
{
    public string MachineName { get; set; } = "";
    public string JobId { get; set; } = "";
    public string LogLevel { get; set; } = ""; // "Error", ...
    public string Message { get; set; } = "";
    public DateTimeOffset Timestamp { get; set; }
}

public sealed class ModifiedItemsResponse
{
    // API field is "Status": "Success" | "Failure"
    [JsonPropertyName("Status")]
    public string Status { get; set; } = "";

    public List<ModifiedItem> Items { get; set; } = new();

    public List<string> ErrorItemIds { get; set; } = new();

    // In your sample this looks like epoch seconds ? keep as long
    public long LastRunTime { get; set; }

    public string TenantId { get; set; } = "";

    // Convenience bool
    [JsonIgnore]
    public bool Success => string.Equals(Status, "Success", StringComparison.OrdinalIgnoreCase);
}

public sealed class ModifiedItem
{
    public string ItemId { get; set; } = "";
    public string FileName { get; set; } = "";
    public string FolderPath { get; set; } = "";
    public string FilePath { get; set; } = "";
    public string FileId { get; set; } = "";

    // ISO-8601 ? DateTimeOffset
    public DateTimeOffset ModifiedAt { get; set; }

    // Milliseconds since epoch ? long
    public long ModifiedAtEpochMs { get; set; }

    // Convenience accessors
    [JsonIgnore]
    public DateTimeOffset ModifiedAtFromMs => DateTimeOffset.FromUnixTimeMilliseconds(ModifiedAtEpochMs);

    [JsonIgnore]
    public long ModifiedAtEpochSeconds => ModifiedAt.ToUnixTimeSeconds();
}

public sealed class DamItemDetails
{
    public string ListItemId { get; set; } = "";
    public string FileName { get; set; } = "";
    public long? ModifiedAt { get; set; }      // epoch seconds? ticks? update when known
    public string? FolderId { get; set; }
    public long? FileSize { get; set; }
    public Dictionary<string, object>? Metadata { get; set; }
}
