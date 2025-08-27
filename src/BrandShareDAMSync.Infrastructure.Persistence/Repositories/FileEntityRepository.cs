using BrandshareDamSync.Abstractions;
using BrandshareDamSync.Domain;
using BrandshareDamSync.Infrastructure.Persistence.Data;

namespace BrandshareDamSync.Infrastructure.Persistence.Repositories;
public class FileEntityRepository(DamSyncDbContext context) : SqlRepository<FileEntity>(context), IFileEntityRepository
{
}
