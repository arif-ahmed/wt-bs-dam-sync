using BrandshareDamSync.Domain;
using BrandshareDamSync.Domain.Interfaces;
using BrandshareDamSync.Infrastructure.Persistence.Data.EntityConfigurations;
using BrandshareDamSync.Infrastructure.Persistence.Data.Interceptors;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;
using System.Runtime.ConstrainedExecution;

namespace BrandshareDamSync.Infrastructure.Persistence.Data;

public sealed class DamSyncDbContext : DbContext
{
    public DamSyncDbContext(DbContextOptions<DamSyncDbContext> options) : base(options) { }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder) 
    {
        base.OnConfiguring(optionsBuilder);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // modelBuilder.Entity<Tenant>().HasData(Seeder.SeedTenants());
        modelBuilder.ApplyConfiguration(new FolderConfiguration());
        modelBuilder.ApplyConfiguration(new FileEntityConfiguration());
        base.OnModelCreating(modelBuilder);

        // Apply: e => !e.IsDeleted to every entity that derives from EntityBase
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            var clrType = entityType.ClrType;
            if (typeof(EntityBase).IsAssignableFrom(clrType))
            {
                var parameter = Expression.Parameter(clrType, "e");
                var prop = Expression.Property(parameter, nameof(EntityBase.IsDeleted));
                var body = Expression.Equal(prop, Expression.Constant(false));
                var lambda = Expression.Lambda(body, parameter);

                modelBuilder.Entity(clrType).HasQueryFilter(lambda);
            }
        }
    }

    public DbSet<Tenant> Tenants { get; set; } = null!;
    public DbSet<SyncJob> Jobs { get; set; } = null!;
    public DbSet<Folder> Folders { get; set; } = null!;
    public DbSet<FileEntity> Files { get; set; } = null!;
    public override Task<int> SaveChangesAsync(
    bool acceptAllChangesOnSuccess,
    CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;

        foreach (var entry in ChangeTracker.Entries())
        {
            if (entry.State == EntityState.Detached || entry.State == EntityState.Unchanged)
                continue;

            // Auditing
            if (entry.Entity is IAuditable auditable)
            {
                if (entry.State == EntityState.Added)
                {
                    auditable.CreatedAtUtc = now;
                    auditable.LastModifiedAtUtc = now;
                }
                else if (entry.State == EntityState.Modified)
                {
                    // prevent changing CreatedAtUtc
                    entry.Property(nameof(IAuditable.CreatedAtUtc)).IsModified = false;
                    auditable.LastModifiedAtUtc = now;
                }
                else if (entry.State == EntityState.Deleted)
                {
                    auditable.LastModifiedAtUtc = now;
                }
            }

            if (entry.State == EntityState.Added && entry.Entity is EntityBase entityBase)
            {
                if (string.IsNullOrEmpty(entityBase.Id))
                    entityBase.Id = Guid.NewGuid().ToString("N");
            }

            // Tenant stamping
            if (entry.State == EntityState.Added && entry.Entity is ITenant tenantScoped)
            {
                if (string.IsNullOrEmpty(tenantScoped.TenantId))
                    tenantScoped.TenantId = "CURRENT_TENANT_ID"; // inject or resolve as needed
            }

            // Soft delete
            if (entry.State == EntityState.Deleted && entry.Entity is EntityBase baseEntity)
            {
                baseEntity.IsDeleted = true;
                entry.State = EntityState.Modified;
            }
        }

        return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

}
