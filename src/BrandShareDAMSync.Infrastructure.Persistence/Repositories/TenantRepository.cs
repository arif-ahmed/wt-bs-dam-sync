using BrandshareDamSync.Abstractions;
using BrandshareDamSync.Domain;
using BrandshareDamSync.Infrastructure.Persistence.Data;

namespace BrandshareDamSync.Infrastructure.Persistence.Repositories;

public sealed class TenantRepository(DamSyncDbContext context) : SqlRepository<Tenant>(context), ITenantRepository
{
}
