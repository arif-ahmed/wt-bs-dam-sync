using BrandshareDamSync.Domain;
using BrandshareDamSync.Infrastructure.BrandShareDamClient;

namespace BrandshareDamSync.Daemon.Mappers;
public class FileEntityMapper
{
    public static FileEntity ToEntity(ModifiedItem listItem, string directoryId, string tenantId, string lastSeenSyncId)
    {
        var nowUtc = DateTime.UtcNow;
        return new FileEntity
        {
            Id = listItem.ItemId,
            FileName = listItem.FileName,
            FilePath = listItem.FilePath,
            FileId = listItem.FileId,
            DirectoryId = directoryId,
            ModifiedAt = listItem.ModifiedAt,
            ModifiedAtEpochMs = listItem.ModifiedAtEpochMs,
            CreatedAtUtc = nowUtc,
            LastModifiedAtUtc = nowUtc,
            TenantId = tenantId,
            LastSeenSyncId = lastSeenSyncId
        };
    }
}
