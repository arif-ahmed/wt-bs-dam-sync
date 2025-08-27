using BrandshareDamSync.Abstractions;
using BrandshareDamSync.Domain;
using BrandshareDamSync.Infrastructure.Persistence.Data;
using BrandshareDamSync.Infrastructure.Persistence.Repositories;
using Microsoft.Extensions.DependencyInjection;


namespace BrandshareDamSync.Infrastructure.Persistence;

public class UnitOfWork : IUnitOfWork, IDisposable
{
    private readonly DamSyncDbContext _context;
    private readonly IServiceProvider _serviceProvider;
    private readonly Dictionary<Type, object> _repositories = new();

    public UnitOfWork(DamSyncDbContext context, IServiceProvider serviceProvider)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
    }

    #region Expose repositories as properties
    public IJobRepository JobRepository => GetRepository<IJobRepository, JobRepository>();
    public ITenantRepository TenantRepository => GetRepository<ITenantRepository, TenantRepository>();
    public IDirectoryRepository DirectoryRepository => GetRepository<IDirectoryRepository, DirectoryRepository>();
    public IFileEntityRepository FileEntityRepository => GetRepository<IFileEntityRepository, FileEntityRepository>();
    #endregion

    public int Commit() => _context.SaveChanges();

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) => await _context.SaveChangesAsync(cancellationToken);

    public void Dispose()
    {
        _context.Dispose();
        GC.SuppressFinalize(this);
    }

    public IRepository<TEntity> GetRepository<TEntity>() where TEntity : EntityBase
    {
        var type = typeof(TEntity);

        if (_repositories.TryGetValue(type, out var repo))
            return (IRepository<TEntity>)repo;

        var repository = _serviceProvider.GetRequiredService<IRepository<TEntity>>();
        _repositories[type] = repository;
        return repository;
    }

    private TRepo GetRepository<TRepo, TImpl>() where TRepo : class where TImpl : class, TRepo
    {
        var type = typeof(TRepo);
        if (_repositories.TryGetValue(type, out var repo))
            return (TRepo)repo;

        // Create instance (could use DI/Activator)
        // var repository = (TRepo)Activator.CreateInstance(typeof(TImpl), _context)!;
        var repository = (TRepo)_serviceProvider.GetRequiredService<TRepo>();
        _repositories[type] = repository;
        return repository;
    }

}

