namespace BrandshareDamSync.Domain;
public enum SyncStatus
{
    NotStarted = 0,
    InProgress = 1,
    Completed = 2,
    Failed = 3
}

public enum SyncDirection
{
    L2D = 0,
    D2L = 1,
    L2DD = 2,
    D2LD = 3,
    Both = 4,
    Unknown = 99
}

public enum JobType
{
    Download = 0,
    Upload = 1,
    BiDirectional = 2,
    DownloadAndCleanup = 3,
    UploadAndCleanup = 4
}

/// <summary>
/// Types of filesystem changes we care about.
/// This mirrors FileSystemWatcher but can be extended for our domain.
/// </summary>
public enum FileChangeKind
{
    /// <summary>
    /// A file or directory was created.
    /// </summary>
    Created = 0,

    /// <summary>
    /// A file or directory was modified (contents or metadata).
    /// </summary>
    Changed = 1,

    /// <summary>
    /// A file or directory was deleted.
    /// </summary>
    Deleted = 2,

    /// <summary>
    /// A file or directory was renamed (old path + new path).
    /// </summary>
    Renamed = 3,

    /// <summary>
    /// A change that could not be categorised (e.g., error recovery or scan-detected).
    /// Useful for reconciliation runs.
    /// </summary>
    Unknown = 99
}

/// <summary>
/// Types of directory changes.
/// Mirrors FileSystemWatcher but scoped to folders.
/// </summary>
public enum DirectoryChangeKind
{
    /// <summary>
    /// A directory was created.
    /// </summary>
    Created = 0,

    /// <summary>
    /// A directory's attributes/metadata changed.
    /// (e.g., timestamp, permissions)
    /// </summary>
    Changed = 1,

    /// <summary>
    /// A directory was deleted.
    /// </summary>
    Deleted = 2,

    /// <summary>
    /// A directory was renamed (old path + new path).
    /// </summary>
    Renamed = 3,

    /// <summary>
    /// Used when a change was detected via reconciliation scan,
    /// or cannot be mapped cleanly.
    /// </summary>
    Unknown = 99
}

/// <summary>
/// Lifecycle state of a file or directory tracked in the local database
/// and synchronised with the DAM side.
/// </summary>
public enum TrackedItemState
{
    /// <summary>
    /// Item was detected (Created/Changed) locally but not yet
    /// recorded in the DAM.
    /// </summary>
    Pending = 0,

    /// <summary>
    /// Item is queued for processing (waiting for worker).
    /// </summary>
    Queued = 1,

    /// <summary>
    /// Item is being actively processed (uploading, updating metadata, etc.).
    /// </summary>
    Processing = 2,

    /// <summary>
    /// Item has been successfully applied to the DAM.
    /// Local and remote are in sync.
    /// </summary>
    Synced = 3,

    /// <summary>
    /// Item was deleted locally and this deletion has been confirmed
    /// in the DAM (a tombstone may be stored for history).
    /// </summary>
    Deleted = 4,

    /// <summary>
    /// Item failed to process (e.g., DAM error, network issue).
    /// It will be retried or manually resolved.
    /// </summary>
    Failed = 5,

    /// <summary>
    /// Item is skipped due to rules (ignore filters, unsupported type, etc.).
    /// </summary>
    Ignored = 6
}

/// <summary>
/// State of a synchronisation job (as a whole).
/// Represents the current lifecycle stage.
/// </summary>
public enum SyncJobStatus
{
    /// <summary>
    /// Job has been created but not started yet.
    /// </summary>
    Created = 0,

    /// <summary>
    /// Job is actively synchronising.
    /// </summary>
    Running = 1,

    /// <summary>
    /// Job finished successfully, all tasks completed.
    /// </summary>
    Succeeded = 2,

    /// <summary>
    /// Job finished but failed (nothing or not all tasks applied).
    /// </summary>
    Failed = 3,

    /// <summary>
    /// Job was cancelled before it could complete.
    /// </summary>
    Cancelled = 4,

    /// <summary>
    /// Used when a change was detected via reconciliation scan,
    /// or cannot be mapped cleanly.
    /// </summary>
    Unknown = 99

}

public enum SyncActionType
{
    Redownload,
    Rename,
    Move,
    NoOp
}

public record SyncDecision(SyncActionType Action, string? Reason = null);

/// <summary>
/// Policy for handling deletions in bi-directional synchronization.
/// Determines how deleted files and directories are handled when syncing from local to DAM.
/// </summary>
public enum DeletionPolicy
{
    /// <summary>
    /// Only mark items as soft-deleted in local tracking (keep DAM items intact).
    /// This is the safest option as it preserves data in the DAM system.
    /// </summary>
    SoftDeleteOnly = 0,

    /// <summary>
    /// Remove local tracking entirely but don't delete from DAM.
    /// This cleans up local database but preserves DAM data.
    /// </summary>
    RemoveTrackingOnly = 1,

    /// <summary>
    /// Delete from both local tracking and DAM system.
    /// WARNING: This permanently removes data from the DAM system.
    /// </summary>
    DeleteFromDAM = 2,

    /// <summary>
    /// Ignore deletions completely - no action taken.
    /// Useful when deletion sync is not desired.
    /// </summary>
    Ignore = 3
}
