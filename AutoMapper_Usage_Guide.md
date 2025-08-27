# AutoMapper Integration for BrandShareDAMSync.Daemon

## Installation

AutoMapper has been successfully installed in the BrandShareDAMSync.Daemon project with the following packages:

```xml
<PackageReference Include="AutoMapper" Version="12.0.1" />
<PackageReference Include="AutoMapper.Extensions.Microsoft.DependencyInjection" Version="12.0.1" />
```

## Configuration

### 1. AutoMapper Profile

Created `DamApiMappingProfile.cs` in the `Mappers` folder that defines mappings between:

- `ModifiedItem` → `FileEntity`
- `DamItem` → `FileEntity`  
- `FolderItem` → `Folder`

### 2. Dependency Injection Setup

AutoMapper is registered in `Program.cs`:

```csharp
// Add AutoMapper configuration
builder.Services.AddAutoMapper(typeof(DamApiMappingProfile));
```

### 3. Constructor Injection

The `BiDirectionalSyncJobExecutor` now includes `IMapper` dependency:

```csharp
public BiDirectionalSyncJobExecutor(
    ILogger<BiDirectionalSyncJobExecutor> logger, 
    IBrandShareDamApi api, 
    IUnitOfWork uow, 
    IDownloaderService downloader, 
    IMapper mapper) : IJobExecutorService
```

## Usage Example

### Mapping API Items to FileEntity

```csharp
// Map the API item to FileEntity using AutoMapper
var fileEntity = mapper.Map<FileEntity>(item);

// Set properties that require manual assignment
fileEntity.TenantId = syncJob.TenantId;
fileEntity.LastSeenSyncId = syncId;

// Try to find the corresponding directory for this file
var correspondingFolder = dbDirectoriesRef
    .FirstOrDefault(d => item.FolderPath.StartsWith(d.Path, StringComparison.OrdinalIgnoreCase));

if (correspondingFolder != null)
{
    fileEntity.DirectoryId = correspondingFolder.Id;
}

// Check if the file entity already exists in the database
var existingFileEntity = await uow.FileEntityRepository.GetByIdAsync(item.ItemId, ct);

if (existingFileEntity == null)
{
    // Add new file entity
    await uow.FileEntityRepository.AddAsync(fileEntity, ct);
    logger.LogInformation("Job: {jobId} - Added new file entity: {fileName} (ID: {itemId})", 
        jobId, item.FileName, item.ItemId);
}
else
{
    // Update existing file entity with new data
    mapper.Map(item, existingFileEntity);
    existingFileEntity.TenantId = syncJob.TenantId;
    existingFileEntity.LastSeenSyncId = syncId;
    existingFileEntity.LastModifiedAtUtc = DateTimeOffset.UtcNow;
    
    if (correspondingFolder != null)
    {
        existingFileEntity.DirectoryId = correspondingFolder.Id;
    }
    
    await uow.FileEntityRepository.UpdateAsync(existingFileEntity, ct);
    logger.LogInformation("Job: {jobId} - Updated existing file entity: {fileName} (ID: {itemId})", 
        jobId, item.FileName, item.ItemId);
}

// Save all changes to database
await uow.SaveChangesAsync(ct);
```

## Mapping Rules

### ModifiedItem/DamItem → FileEntity

**Automatically Mapped:**
- `ItemId` → `Id`
- `FileName` → `FileName`
- `FolderPath` → `FolderPath`
- `FilePath` → `FilePath`
- `FileId` → `FileId`
- `ModifiedAt` → `ModifiedAt`
- `ModifiedAtEpochMs` → `ModifiedAtEpochMs`

**Set by AutoMapper:**
- `CreatedAtUtc` → Current UTC timestamp
- `LastModifiedAtUtc` → Current UTC timestamp
- `IsDeleted` → `false`

**Must be Set Manually:**
- `TenantId` - From sync job context
- `LastSeenSyncId` - From current sync operation
- `DirectoryId` - Based on folder path matching
- `SizeInBytes` - From file system or API if available
- `ChecksumHash` - From file content if needed

### FolderItem → Folder

**Automatically Mapped:**
- `Id` → `Id`
- `ParentId` → `ParentId`
- `Label` → `Label`
- `Path` → `Path`
- `CreatedAt` → `CreatedAt`
- `ModifiedAt` → `ModifiedAt`

**Set by AutoMapper:**
- `CreatedAtUtc` → Current UTC timestamp
- `LastModifiedAtUtc` → Current UTC timestamp
- `IsActive` → `true`
- `IsDeleted` → `false`

**Must be Set Manually:**
- `TenantId` - From sync job context
- `LastSeenSyncId` - From current sync operation

## Benefits

1. **Reduced Boilerplate**: Eliminates manual property assignment for common mappings
2. **Type Safety**: Compile-time checking of property mappings
3. **Maintainability**: Centralized mapping configuration
4. **Consistency**: Standardized mapping rules across the application
5. **Performance**: Optimized mapping operations using expression trees

## Implementation Status

✅ AutoMapper packages installed
✅ Mapping profile created
✅ DI container configured
✅ BiDirectionalSyncJobExecutor updated to use AutoMapper
✅ File entities are mapped and saved to database
✅ Build verification completed

The integration is now complete and ready for use in the DAM synchronization process.