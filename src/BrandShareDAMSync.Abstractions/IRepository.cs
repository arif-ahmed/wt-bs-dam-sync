using System.Linq.Expressions;

namespace BrandshareDamSync.Abstractions;

public interface IRepository<TEntity> where TEntity : class
{
    Task<TEntity?> GetByIdAsync(string id, CancellationToken cancellationToken = default);
    Task<IEnumerable<TEntity>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<(IEnumerable<TEntity>, int)> FindAsync(Expression<Func<TEntity, bool>>? predicate = null, int pageNumber = 1, int pageSize = 100, CancellationToken cancellationToken = default);
    Task AddAsync(TEntity entity, CancellationToken cancellationToken = default);
    Task UpdateAsync(TEntity entity, CancellationToken cancellationToken = default);
    Task DeleteAsync(string id, CancellationToken cancellationToken = default);
    //Task<(IEnumerable<TEntity>, int)> GetAsync(Func<IQueryable<TEntity>, IQueryable<TEntity>>? query = null, CancellationToken cancellationToken = default);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
    IQueryable<TEntity> Query(); 
    Task<(IReadOnlyList<TEntity> Items, int TotalCount)> GetAsync(
        Expression<Func<TEntity, bool>>? predicate = default,
        Func<IQueryable<TEntity>, IQueryable<TEntity>>? include = default,
        Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>? orderBy = default,
        int? pageNumber = default,
        int? pageSize = default,
        bool asNoTracking = true,
        bool asSplitQuery = true,
        CancellationToken cancellationToken = default);

    Task<(IReadOnlyList<TResult> Items, int TotalCount)> GetAsync<TResult>(
            Expression<Func<TEntity, bool>>? predicate = default,
            Func<IQueryable<TEntity>, IQueryable<TEntity>>? include = default,
            Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>? orderBy = default,
            Expression<Func<TEntity, TResult>> selector = default!, // required
            int? pageNumber = default,
            int? pageSize = default,
            bool asNoTracking = true,
            bool asSplitQuery = true,
            CancellationToken cancellationToken = default);
}
