using AutoMapper;
using BrandshareDamSync.Domain;
using BrandshareDamSync.Infrastructure.BrandShareDamClient;

namespace BrandshareDamSync.Daemon.Mappers;

/// <summary>
/// AutoMapper profile for mapping between DAM API models and domain entities.
/// </summary>
public class DamApiMappingProfile : Profile
{
    public DamApiMappingProfile()
    {
        // Map from ModifiedItem to FileEntity
        CreateMap<ModifiedItem, FileEntity>()
            .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.ItemId))
            .ForMember(dest => dest.FileName, opt => opt.MapFrom(src => src.FileName))
            .ForMember(dest => dest.FolderPath, opt => opt.MapFrom(src => src.FolderPath))
            .ForMember(dest => dest.FilePath, opt => opt.MapFrom(src => src.FilePath))
            .ForMember(dest => dest.FileId, opt => opt.MapFrom(src => src.FileId))
            .ForMember(dest => dest.ModifiedAt, opt => opt.MapFrom(src => src.ModifiedAt))
            .ForMember(dest => dest.ModifiedAtEpochMs, opt => opt.MapFrom(src => src.ModifiedAtEpochMs))
            .ForMember(dest => dest.CreatedAtUtc, opt => opt.MapFrom(src => DateTimeOffset.UtcNow))
            .ForMember(dest => dest.LastModifiedAtUtc, opt => opt.MapFrom(src => DateTimeOffset.UtcNow))
            // Properties that need to be set by the caller
            .ForMember(dest => dest.TenantId, opt => opt.Ignore())
            .ForMember(dest => dest.LastSeenSyncId, opt => opt.Ignore())
            .ForMember(dest => dest.DirectoryId, opt => opt.Ignore())
            .ForMember(dest => dest.SizeInBytes, opt => opt.Ignore())
            .ForMember(dest => dest.ChecksumHash, opt => opt.Ignore())
            .ForMember(dest => dest.IsDeleted, opt => opt.MapFrom(src => false))
            // Navigation properties
            .ForMember(dest => dest.Tenant, opt => opt.Ignore())
            .ForMember(dest => dest.Folder, opt => opt.Ignore());

        // Map from DamItem to FileEntity
        CreateMap<DamItem, FileEntity>()
            .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.ItemId))
            .ForMember(dest => dest.FileName, opt => opt.MapFrom(src => src.FileName))
            .ForMember(dest => dest.FolderPath, opt => opt.MapFrom(src => src.FolderPath))
            .ForMember(dest => dest.FilePath, opt => opt.MapFrom(src => src.FilePath))
            .ForMember(dest => dest.FileId, opt => opt.MapFrom(src => src.FileId))
            .ForMember(dest => dest.ModifiedAt, opt => opt.MapFrom(src => (DateTimeOffset)src.ModifiedAt))
            .ForMember(dest => dest.ModifiedAtEpochMs, opt => opt.MapFrom(src => src.ModifiedAtEpochMs))
            .ForMember(dest => dest.CreatedAtUtc, opt => opt.MapFrom(src => DateTimeOffset.UtcNow))
            .ForMember(dest => dest.LastModifiedAtUtc, opt => opt.MapFrom(src => DateTimeOffset.UtcNow))
            // Properties that need to be set by the caller
            .ForMember(dest => dest.TenantId, opt => opt.Ignore())
            .ForMember(dest => dest.LastSeenSyncId, opt => opt.Ignore())
            .ForMember(dest => dest.DirectoryId, opt => opt.Ignore())
            .ForMember(dest => dest.SizeInBytes, opt => opt.Ignore())
            .ForMember(dest => dest.ChecksumHash, opt => opt.Ignore())
            .ForMember(dest => dest.IsDeleted, opt => opt.MapFrom(src => false))
            // Navigation properties
            .ForMember(dest => dest.Tenant, opt => opt.Ignore())
            .ForMember(dest => dest.Folder, opt => opt.Ignore());

        // Map from FolderItem to Folder
        CreateMap<FolderItem, Folder>()
            .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => src.CreatedAt))
            .ForMember(dest => dest.ModifiedAt, opt => opt.MapFrom(src => src.ModifiedAt))
            .ForMember(dest => dest.CreatedAtUtc, opt => opt.MapFrom(src => DateTimeOffset.UtcNow))
            .ForMember(dest => dest.LastModifiedAtUtc, opt => opt.MapFrom(src => DateTimeOffset.UtcNow))
            // Properties that need to be set by the caller
            .ForMember(dest => dest.TenantId, opt => opt.Ignore())
            .ForMember(dest => dest.LastSeenSyncId, opt => opt.Ignore())
            .ForMember(dest => dest.IsDeleted, opt => opt.MapFrom(src => false))
            // Navigation properties
            .ForMember(dest => dest.Tenant, opt => opt.Ignore())
            .ForMember(dest => dest.Files, opt => opt.Ignore());
    }
}