using Refit;

namespace BrandshareDamSync.Infrastructure.BrandShareDamClient;

public sealed class GetItemsByFolderIdQuery
{
    [AliasAs("jobId")]
    public string JobId { get; set; } = "";

    [AliasAs("isActive")]
    public bool IsActive { get; set; }

    [AliasAs("folderId")]
    public string FolderId { get; set; } = "";

    [AliasAs("lastItemId")]
    public string? LastItemId { get; set; }

    [AliasAs("pageSize")]
    public int? PageSize { get; set; }

    // The server expects Unix epoch in MILLISECONDS (per your comment)
    [AliasAs("modifiedAfterAt")]
    public long? ModifiedAfterAt { get; set; }
}
