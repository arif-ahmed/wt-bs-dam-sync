using BrandshareDamSync.Abstractions;
using BrandshareDamSync.Domain;
using BrandshareDamSync.Infrastructure.Persistence.Data;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace BrandshareDamSync.Infrastructure.Persistence;

public class SqlRepository<TEntity>(DamSyncDbContext context) : IRepository<TEntity> where TEntity : EntityBase
{
    protected readonly DbSet<TEntity> _dbSet = context.Set<TEntity>()
        ?? throw new ArgumentNullException(nameof(_dbSet));

    // Expose composable queryable
    public IQueryable<TEntity> Query() => _dbSet.AsQueryable();

    public virtual async Task AddAsync(TEntity entity, CancellationToken cancellationToken = default)
    {
        await _dbSet.AddAsync(entity, cancellationToken);
    }

    public virtual async Task DeleteAsync(string id, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(id))
        {
            throw new ArgumentNullException(nameof(id), "Id cannot be null or empty.");
        }
        var entity = await _dbSet.FindAsync(id);
        if (entity == null)
        {
            throw new KeyNotFoundException($"Entity with id {id} not found.");
        }
        _dbSet.Remove(entity);
    }

    public virtual async Task<(IEnumerable<TEntity>, int)> FindAsync(Expression<Func<TEntity, bool>>? predicate = null, int pageNumber = 1, int pageSize = 100, CancellationToken cancellationToken = default)
    {

        if (predicate == null)
        {
            throw new ArgumentNullException(nameof(predicate), "Predicate cannot be null.");
        }

        var query = _dbSet.Where(predicate);

        int totalCount = await query.CountAsync();

        if (pageNumber > 0)
        {
            query = query.Skip((pageNumber - 1) * pageSize);
        }
        if (pageNumber > 0)
        {
            query = query.Take(pageSize);
        }

        return (await query.ToListAsync(cancellationToken), totalCount);
    }

    public virtual async Task<IEnumerable<TEntity>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _dbSet.ToListAsync();
    }

    public async Task<(IReadOnlyList<TEntity> Items, int TotalCount)> GetAsync(
        Expression<Func<TEntity, bool>>? predicate = default,
        Func<IQueryable<TEntity>, IQueryable<TEntity>>? include = default,
        Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>? orderBy = default,
        int? pageNumber = default,
        int? pageSize = default,
        bool asNoTracking = true,
        bool asSplitQuery = true,
        CancellationToken cancellationToken = default)
    {
        IQueryable<TEntity> query = _dbSet;

        if (predicate is not null)
            query = query.Where(predicate);

        if (include is not null)
            query = include(query);

        if (asNoTracking)
            query = query.AsNoTracking();

        if (asSplitQuery)
            query = query.AsSplitQuery();

        var totalCount = await query.CountAsync(cancellationToken);

        if (orderBy is not null)
            query = orderBy(query);

        if (pageNumber.HasValue && pageSize.HasValue && pageNumber > 0 && pageSize > 0)
            query = query.Skip((pageNumber.Value - 1) * pageSize.Value)
                         .Take(pageSize.Value);

        var items = await query.ToListAsync(cancellationToken);
        return (items, totalCount);
    }

    // PROJECTION OVERLOAD
    public async Task<(IReadOnlyList<TResult> Items, int TotalCount)> GetAsync<TResult>(
        Expression<Func<TEntity, bool>>? predicate = default,
        Func<IQueryable<TEntity>, IQueryable<TEntity>>? include = default,
        Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>? orderBy = default,
        Expression<Func<TEntity, TResult>> selector = default!, // required
        int? pageNumber = default,
        int? pageSize = default,
        bool asNoTracking = true,
        bool asSplitQuery = true,
        CancellationToken cancellationToken = default)
    {
        if (selector is null)
            throw new ArgumentNullException(nameof(selector));

        IQueryable<TEntity> query = _dbSet;

        if (predicate is not null)
            query = query.Where(predicate);

        if (include is not null)
            query = include(query);

        if (asNoTracking)
            query = query.AsNoTracking();

        if (asSplitQuery)
            query = query.AsSplitQuery();

        var totalCount = await query.CountAsync(cancellationToken);

        if (orderBy is not null)
            query = orderBy(query);

        var projected = query.Select(selector);

        if (pageNumber.HasValue && pageSize.HasValue && pageNumber > 0 && pageSize > 0)
            projected = projected
                .Skip((pageNumber.Value - 1) * pageSize.Value)
                .Take(pageSize.Value);

        var items = await projected.ToListAsync(cancellationToken);
        return (items, totalCount);
    }



    public virtual async Task<TEntity?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(id))
        {
            throw new ArgumentNullException(nameof(id), "Id cannot be null or empty.");
        }
        var entity = await _dbSet.FindAsync(id);
        //if (entity == null)
        //{
        //    throw new KeyNotFoundException($"Entity with id {id} not found.");
        //}
        return entity;
    }

    public virtual async Task UpdateAsync(TEntity entity, CancellationToken cancellationToken = default)
    {
        if (entity == null)
            throw new ArgumentNullException(nameof(entity));

        // Find key property info safely
        var entityType = context.Model.FindEntityType(typeof(TEntity));
        if (entityType == null)
            throw new InvalidOperationException($"Could not find entity type {typeof(TEntity).Name} in the context.");

        var keyProperty = entityType.FindPrimaryKey()?.Properties?.FirstOrDefault();
        if (keyProperty == null)
            throw new InvalidOperationException($"No primary key defined for entity type {typeof(TEntity).Name}.");

        var keyName = keyProperty.Name;
        var keyPropInfo = typeof(TEntity).GetProperty(keyName);
        if (keyPropInfo == null)
            throw new InvalidOperationException($"Entity type {typeof(TEntity).Name} does not have a property named {keyName}.");

        var keyValue = keyPropInfo.GetValue(entity);
        if (keyValue == null)
            throw new InvalidOperationException("Key value cannot be null.");

        // Retrieve the existing entity from the database
        var existingEntity = await _dbSet.FindAsync(keyValue);
        if (existingEntity == null)
            throw new InvalidOperationException("Entity not found in database.");

        // Iterate over properties and update values
        foreach (var property in typeof(TEntity).GetProperties())
        {
            // Skip key and non-writable properties
            if (property.Name == keyName || !property.CanWrite)
                continue;

            var newValue = property.GetValue(entity);
            var oldValue = property.GetValue(existingEntity);

            // Only update if different
            if (!Equals(newValue, oldValue))
            {
                property.SetValue(existingEntity, newValue);
            }
        }
    }

    public virtual async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        await context.SaveChangesAsync(cancellationToken);
    }
}
