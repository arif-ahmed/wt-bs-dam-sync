using BrandshareDamSync.Domain.Interfaces;
using Newtonsoft.Json;

namespace BrandshareDamSync.Domain;

/// <summary>
/// Represents a synchronization job configuration that defines how files are synchronized
/// between the local filesystem and the DAM system.
/// </summary>
/// <remarks>
/// A sync job contains all necessary configuration for automated file synchronization,
/// including source/destination paths, sync direction, scheduling, and tenant association.
/// Jobs can operate in various modes: one-way upload, one-way download, or bi-directional sync.
/// </remarks>
public sealed class SyncJob : EntityBase, ITenant, IAuditable
{
    /// <summary>
    /// Gets or sets the display name for this synchronization job.
    /// </summary>
    /// <value>A human-readable name that identifies the purpose of this sync job.</value>
    [JsonProperty("JobName")]
    public string JobName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the name of the DAM volume being synchronized.
    /// </summary>
    /// <value>The volume identifier as known by the DAM system.</value>
    [JsonProperty("VolumeName")]
    public string VolumeName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the path within the DAM volume for synchronization.
    /// </summary>
    /// <value>The relative path within the DAM volume that will be synchronized.</value>
    [JsonProperty("VolumePath")]
    public string VolumePath { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the unique identifier of the DAM volume.
    /// </summary>
    /// <value>The DAM system's internal identifier for the volume.</value>
    [JsonProperty("VolumeId")]
    public string VolumeId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the local filesystem path where files will be synchronized.
    /// </summary>
    /// <value>The absolute path on the local machine for file synchronization.</value>
    [JsonProperty("DestinationPath")]
    public string DestinationPath { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the interval in minutes between automatic synchronization runs.
    /// </summary>
    /// <value>The number of minutes to wait between sync operations, or null for manual-only sync.</value>
    [JsonProperty("JobInterVal")]
    public int? JobIntervalMinutes { get; set; }

    /// <summary>
    /// Gets or sets the direction of file synchronization.
    /// </summary>
    /// <value>A <see cref="SyncDirection"/> value indicating how files should be synchronized.</value>
    [JsonProperty("SyncDirection")]
    public SyncDirection SyncDirection { get; set; }

    /// <summary>
    /// Gets or sets the current status of the synchronization job.
    /// </summary>
    /// <value>A string representation of the job's current operational status.</value>
    [JsonProperty("JobStatus")]
    public string JobStatus { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets a value indicating whether this synchronization job is active.
    /// </summary>
    /// <value><c>true</c> if the job should be processed by the sync daemon; otherwise, <c>false</c>.</value>
    [JsonProperty("IsActive")]
    public bool IsActive { get; set; }

    /// <summary>
    /// Gets or sets the primary location for conflict resolution in bi-directional sync.
    /// </summary>
    /// <value>Indicates which location (Local, DAM, or Both) takes precedence during conflicts.</value>
    [JsonProperty("PrimaryLocation")]
    public string PrimaryLocation { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the detailed status of the synchronization job.
    /// </summary>
    /// <value>A <see cref="SyncJobStatus"/> enumeration value providing detailed job status.</value>
    [JsonProperty("SyncJobStatus")]
    public SyncJobStatus SyncJobStatus { get; set; }

    /// <summary>
    /// Gets or sets the identifier of the last processed item for pagination.
    /// </summary>
    /// <value>Used for resuming synchronization from a specific point in large datasets.</value>
    public string? LastItemId { get; set; }
    
    /// <summary>
    /// Gets or sets the timestamp of the last synchronization run.
    /// </summary>
    /// <value>Unix timestamp in milliseconds representing when the job last executed.</value>
    public long LastRunTime { get; set; }
    
    /// <summary>
    /// Gets or sets the identifier of the tenant this job belongs to.
    /// </summary>
    /// <value>The tenant ID for multi-tenant isolation.</value>
    public string TenantId { get; set; } = default!;

    /// <summary>
    /// Gets or sets the timestamp when this job was created.
    /// </summary>
    /// <value>UTC timestamp of job creation.</value>
    public DateTimeOffset CreatedAtUtc { get; set; }
    
    /// <summary>
    /// Gets or sets the timestamp when this job was last modified.
    /// </summary>
    /// <value>UTC timestamp of the most recent modification.</value>
    public DateTimeOffset LastModifiedAtUtc { get; set; }

    /// <summary>
    /// Gets or sets the deletion policy for files when syncing from local to DAM.
    /// </summary>
    /// <value>A <see cref="DeletionPolicy"/> value indicating how to handle deleted files.</value>
    [JsonProperty("FileDeletionPolicy")]
    public DeletionPolicy FileDeletionPolicy { get; set; } = DeletionPolicy.SoftDeleteOnly;

    /// <summary>
    /// Gets or sets the deletion policy for directories when syncing from local to DAM.
    /// </summary>
    /// <value>A <see cref="DeletionPolicy"/> value indicating how to handle deleted directories.</value>
    [JsonProperty("DirectoryDeletionPolicy")]
    public DeletionPolicy DirectoryDeletionPolicy { get; set; } = DeletionPolicy.SoftDeleteOnly;

    #region navigation properties
    /// <summary>
    /// Gets or sets the tenant associated with this synchronization job.
    /// </summary>
    /// <value>The <see cref="Tenant"/> entity this job belongs to.</value>
    public Tenant Tenant { get; set; } = default!;
    #endregion
}


