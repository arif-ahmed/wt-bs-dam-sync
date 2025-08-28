using BrandshareDamSync.Domain.Interfaces;

namespace BrandshareDamSync.Domain;

public class Folder : EntityBase, ITenant, IAuditable
{
    public bool IsActive { get; set; }
    public string? ParentId { get; set; }
    public string Label { get; set; } = string.Empty;
    public string Path { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset ModifiedAt { get; set; }

    public string NormalisedPath => Path?.Replace("\\", "/") ?? string.Empty;
    public string[] Segments => NormalisedPath.Trim('/').Split('/', StringSplitOptions.RemoveEmptyEntries);

    public string TenantId { get; set; } = default!;
    public DateTimeOffset CreatedAtUtc { get; set; } = default!;
    public DateTimeOffset LastModifiedAtUtc { get; set; } = default!;

    public string LastSeenSyncId { get; set; } = default!;

    #region navigation properties
    public Tenant Tenant { get; set; } = default!;
    public ICollection<FileEntity> Files { get; set; } = new List<FileEntity>();
    #endregion

    public void Deconstruct(out string id, out string path)
    {
        id = this.Id;
        path = this.Path;
    }
}
