using BrandshareDamSync.Domain.Interfaces;

namespace BrandshareDamSync.Domain;

/// <summary>
/// Represents a file entity that tracks metadata and synchronization state for files
/// managed by the DAM sync system.
/// </summary>
/// <remarks>
/// This entity maintains comprehensive metadata about files including their location,
/// modification history, checksum for integrity verification, and synchronization tracking.
/// It serves as the primary mechanism for delta synchronization and conflict detection.
/// </remarks>
public class FileEntity : EntityBase, ITenant, IAuditable
{
    /// <summary>
    /// Gets or sets the name of the file including its extension.
    /// </summary>
    /// <value>The complete filename as it appears in the filesystem.</value>
    public string FileName { get; set; } = default!;
    
    /// <summary>
    /// Gets or sets the path of the folder containing this file.
    /// </summary>
    /// <value>The directory path relative to the sync root where this file is located.</value>
    public string FolderPath { get; set; } = default!;
    
    /// <summary>
    /// Gets or sets the complete file path including filename.
    /// </summary>
    /// <value>The full path to the file, or null if not applicable.</value>
    public string? FilePath { get; set; }
    
    /// <summary>
    /// Gets or sets the unique identifier for this file in the DAM system.
    /// </summary>
    /// <value>The DAM's internal file identifier, or null if not yet uploaded.</value>
    public string? FileId { get; set; }
    
    /// <summary>
    /// Gets or sets the timestamp when this file was last modified.
    /// </summary>
    /// <value>The modification timestamp as a DateTimeOffset.</value>
    public DateTimeOffset ModifiedAt { get; set; }
    
    /// <summary>
    /// Gets or sets the modification timestamp in epoch milliseconds.
    /// </summary>
    /// <value>Unix timestamp in milliseconds for compatibility with DAM APIs.</value>
    public long ModifiedAtEpochMs { get; set; }
    
    /// <summary>
    /// Gets or sets the identifier of the directory containing this file.
    /// </summary>
    /// <value>The unique identifier of the parent folder, or null if orphaned.</value>
    public string? DirectoryId { get; set; }

    /// <summary>
    /// Gets or sets the size of the file in bytes.
    /// </summary>
    /// <value>The file size in bytes for storage and transfer planning.</value>
    public long SizeInBytes { get; set; }
    
    /// <summary>
    /// Gets or sets the checksum hash of the file content.
    /// </summary>
    /// <value>A hash value (typically SHA-256) for integrity verification, or null if not calculated.</value>
    public string? ChecksumHash { get; set; }

    /// <summary>
    /// Gets or sets the identifier of the tenant this file belongs to.
    /// </summary>
    /// <value>The tenant ID for multi-tenant data isolation.</value>
    public string TenantId { get; set; } = default!;
    
    /// <summary>
    /// Gets or sets the timestamp when this file entity was created.
    /// </summary>
    /// <value>UTC timestamp when the entity was first tracked.</value>
    public DateTimeOffset CreatedAtUtc { get; set; } = default!;
    
    /// <summary>
    /// Gets or sets the timestamp when this file entity was last modified.
    /// </summary>
    /// <value>UTC timestamp of the most recent entity update.</value>
    public DateTimeOffset LastModifiedAtUtc { get; set; } = default!;

    /// <summary>
    /// Gets or sets the identifier of the last synchronization run that processed this file.
    /// </summary>
    /// <value>Used to identify files that need processing in the current sync cycle.</value>
    public string LastSeenSyncId { get; set; } = default!;

    #region navigation properties
    /// <summary>
    /// Gets or sets the tenant associated with this file entity.
    /// </summary>
    /// <value>The <see cref="Tenant"/> this file belongs to.</value>
    public Tenant Tenant { get; set; } = default!;
    
    /// <summary>
    /// Gets or sets the folder that contains this file.
    /// </summary>
    /// <value>The <see cref="Folder"/> entity representing the parent directory.</value>
    public Folder Folder { get; set; } = default!;
    #endregion
}
