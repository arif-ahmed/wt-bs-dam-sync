using BrandshareDamSync.Abstractions;
using BrandshareDamSync.Domain;
using BrandshareDamSync.Infrastructure.Persistence.Data;

namespace BrandshareDamSync.Infrastructure.Persistence.Repositories;

public class DirectoryRepository(DamSyncDbContext context) : SqlRepository<Folder>(context), IDirectoryRepository
{
}
