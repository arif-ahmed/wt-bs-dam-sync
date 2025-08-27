using BrandshareDamSync.Domain;

namespace BrandshareDamSync.Abstractions;

public interface IUnitOfWork : IDisposable
{
    IJobRepository JobRepository { get; }
    ITenantRepository TenantRepository { get; }
    IDirectoryRepository DirectoryRepository { get; }
    IFileEntityRepository FileEntityRepository { get; }
    IRepository<TEntity> GetRepository<TEntity>() where TEntity : EntityBase;
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
