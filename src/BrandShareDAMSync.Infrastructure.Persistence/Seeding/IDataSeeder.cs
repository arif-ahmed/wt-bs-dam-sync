namespace BrandshareDamSync.Infrastructure.Persistence.Seeding;

public interface IDataSeeder
{
    Task SeedAsync(CancellationToken ct = default);
}
